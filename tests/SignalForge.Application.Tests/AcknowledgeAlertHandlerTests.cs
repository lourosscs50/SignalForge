using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class AcknowledgeAlertHandlerTests
{
    private const string ActorId = "750e8400-e29b-41d4-a716-446655440000";

    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class FixedCurrentUser(string userId) : ICurrentUser
    {
        public string? UserId => userId;
    }

    private sealed class NullCurrentUser : ICurrentUser
    {
        public string? UserId => null;
    }

    private sealed class FakeAlertRepository : IAlertRepository
    {
        public Alert? Stored { get; set; }
        public int UpdateCount { get; private set; }

        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken)
        {
            UpdateCount++;
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

    private static FakeRuleRepositoryForLifecycle RuleRepoFor(Alert alert) =>
        new()
        {
            Rule = new Rule(alert.RuleId, "T", RuleTypes.SignalTypeEquals, "m", true, false, DateTime.UtcNow)
        };

    private static AlertLifecycleAutomationCoordinator TestCoordinator() =>
        AutomationTestHarness.CreateCoordinator(new CapturingLifecyclePublisher(), new CapturingTriggerPublisher());

    [Fact]
    public async Task HandleAsync_acknowledges_with_actor_and_updates_stored_attribution()
    {
        var t0 = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(t0),
            new FixedCurrentUser(ActorId),
            RuleRepoFor(alert),
            TestCoordinator());

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsAcknowledged);
        Assert.Equal(new DateTimeOffset(t0, TimeSpan.Zero), result.AcknowledgedAtUtc);
        Assert.Equal(ActorId, result.AcknowledgedByUserId);
        Assert.Equal(1, repos.UpdateCount);
        Assert.Equal(ActorId, repos.Stored!.AcknowledgedByUserId);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_alert_missing()
    {
        var repos = new FakeAlertRepository();
        var handler = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(DateTime.UtcNow),
            new NullCurrentUser(),
            new FakeRuleRepositoryForLifecycle
            {
                Rule = new Rule(Guid.NewGuid(), "X", RuleTypes.SignalTypeEquals, "x", true, false, DateTime.UtcNow)
            },
            TestCoordinator());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_throws_when_actor_missing_and_alert_exists()
    {
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(DateTime.UtcNow),
            new NullCurrentUser(),
            RuleRepoFor(alert),
            TestCoordinator());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(id, CancellationToken.None));
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_idempotent_second_ack_no_extra_update()
    {
        var firstAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var ruleRepo = RuleRepoFor(alert);
        var h1 = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(firstAt),
            new FixedCurrentUser(ActorId),
            ruleRepo,
            TestCoordinator());
        var r1 = await h1.HandleAsync(id, CancellationToken.None);
        Assert.Equal(1, repos.UpdateCount);

        var secondAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var h2 = new AcknowledgeAlert.Handler(
            repos,
            new FixedClock(secondAt),
            new FixedCurrentUser("other-user"),
            ruleRepo,
            TestCoordinator());
        var r2 = await h2.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.Equal(r1.AcknowledgedAtUtc, r2.AcknowledgedAtUtc);
        Assert.Equal(ActorId, r2.AcknowledgedByUserId);
        Assert.Equal(1, repos.UpdateCount);
    }
}
