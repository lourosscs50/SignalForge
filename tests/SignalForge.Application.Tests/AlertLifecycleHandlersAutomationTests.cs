using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 6.0–6.3: lifecycle handlers publish events and triggers per policy on real transitions only.</summary>
public sealed class AlertLifecycleHandlersAutomationTests
{
    private const string ActorId = "850e8400-e29b-41d4-a716-446655440000";

    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class User(string id) : ICurrentUser
    {
        public string? UserId => id;
    }

    private sealed class HandlerAlertRepo : IAlertRepository
    {
        public Alert? Stored { get; set; }
        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);
        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken)
        {
            Stored = alert;
            return Task.CompletedTask;
        }

        public Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<Alert>([], query.Page, query.PageSize, 0));

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Stored is null ? [] : [Stored]);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Stored?.RuleId == ruleId ? [Stored!] : []);
    }

    private static Rule RuleFor(Guid ruleId) =>
        new(ruleId, "HR", RuleTypes.SignalTypeEquals, "mv", true, false, DateTime.UtcNow);

    [Fact]
    public async Task Acknowledge_first_call_publishes_lifecycle_only()
    {
        var t = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var repos = new HandlerAlertRepo { Stored = new Alert(id, Guid.NewGuid(), ruleId, t) };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var handler = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(t),
            new User(ActorId),
            new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) },
            coord);

        await handler.HandleAsync(id, CancellationToken.None);

        var e = Assert.Single(life.Events);
        Assert.Equal(AlertLifecycleTransitionType.AlertAcknowledged, e.LifecycleTransitionType);
        Assert.Equal(ActorId, e.AcknowledgedByUserId);
        Assert.Equal(AlertMappings.StatusAcknowledged, e.CurrentStatus);
        Assert.True(e.AcknowledgedAtUtc.HasValue);
        Assert.Empty(trig.Triggers);
    }

    [Fact]
    public async Task Acknowledge_idempotent_second_call_no_additional_publications()
    {
        var t1 = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var repos = new HandlerAlertRepo { Stored = new Alert(id, Guid.NewGuid(), ruleId, t1) };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var ruleRepo = new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) };
        var h1 = new AcknowledgeAlert.Handler(repos, new FixedClock(t1), new User(ActorId), ruleRepo, coord);
        await h1.HandleAsync(id, CancellationToken.None);
        var h2 = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(t1.AddDays(1)),
            new User("other"),
            ruleRepo,
            coord);
        await h2.HandleAsync(id, CancellationToken.None);

        Assert.Single(life.Events);
        Assert.Empty(trig.Triggers);
    }

    [Fact]
    public async Task Resolve_first_call_publishes_lifecycle_only()
    {
        var t = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var repos = new HandlerAlertRepo { Stored = new Alert(id, Guid.NewGuid(), ruleId, t) };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var handler = new ResolveAlert.Handler(
            repos,
            new FixedClock(t),
            new User(ActorId),
            new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) },
            coord);

        await handler.HandleAsync(id, CancellationToken.None);

        var ev = Assert.Single(life.Events);
        Assert.Equal(AlertLifecycleTransitionType.AlertResolved, ev.LifecycleTransitionType);
        Assert.Equal(AlertMappings.StatusResolved, ev.CurrentStatus);
        Assert.Equal(ActorId, ev.ResolvedByUserId);
        Assert.Empty(trig.Triggers);
    }

    [Fact]
    public async Task Resolve_idempotent_no_extra_lifecycle_or_trigger()
    {
        var t = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var repos = new HandlerAlertRepo { Stored = new Alert(id, Guid.NewGuid(), ruleId, t) };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var ruleRepo = new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) };
        var h1 = new ResolveAlert.Handler(repos, new FixedClock(t), new User(ActorId), ruleRepo, coord);
        await h1.HandleAsync(id, CancellationToken.None);
        var resolved = repos.Stored!;
        var h2 = new ResolveAlert.Handler(
            repos,
            new FixedClock(t.AddDays(1)),
            new User("x"),
            ruleRepo,
            coord);
        await h2.HandleAsync(id, CancellationToken.None);

        Assert.Single(life.Events);
        Assert.Empty(trig.Triggers);
        Assert.True(resolved.IsResolved);
    }

    [Fact]
    public async Task Reopen_from_resolved_publishes_lifecycle_and_trigger()
    {
        var t = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var resolved = new Alert(id, Guid.NewGuid(), ruleId, t).Resolve(t.AddMinutes(1), "prev");
        var repos = new HandlerAlertRepo { Stored = resolved };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var handler = new ReopenAlert.Handler(
            repos,
            new FixedClock(t.AddMinutes(5)),
            new User(ActorId),
            new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) },
            coord);

        await handler.HandleAsync(id, CancellationToken.None);

        Assert.Equal(AlertLifecycleTransitionType.AlertReopened, Assert.Single(life.Events).LifecycleTransitionType);
        Assert.Equal(ActorId, life.Events[0].ReopenedByUserId);
        Assert.True(life.Events[0].ReopenedAtUtc.HasValue);
        var tr = Assert.Single(trig.Triggers);
        Assert.Equal(nameof(AlertLifecycleTransitionType.AlertReopened), tr.TriggerType);
        Assert.True(tr.HasBeenReopened);
        Assert.Equal(ActorId, tr.ReopenedByUserId);
        Assert.Equal(AlertMappings.StatusOpen, tr.CurrentStatus);
    }

    [Fact]
    public async Task Reopen_idempotent_when_unresolved_no_publications()
    {
        var t = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var repos = new HandlerAlertRepo { Stored = new Alert(id, Guid.NewGuid(), ruleId, t) };
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var handler = new ReopenAlert.Handler(
            repos,
            new FixedClock(t),
            new User(ActorId),
            new FakeRuleRepositoryForLifecycle { Rule = RuleFor(ruleId) },
            coord);

        await handler.HandleAsync(id, CancellationToken.None);

        Assert.Empty(life.Events);
        Assert.Empty(trig.Triggers);
    }

    [Fact]
    public async Task Missing_alert_no_lifecycle_or_trigger_for_acknowledge()
    {
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var handler = new AcknowledgeAlert.Handler(
            new HandlerAlertRepo(),
            new FixedClock(DateTime.UtcNow),
            new User(ActorId),
            new FakeRuleRepositoryForLifecycle { Rule = RuleFor(Guid.NewGuid()) },
            coord);

        Assert.Null(await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Empty(life.Events);
        Assert.Empty(trig.Triggers);
    }
}
