using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 5.3B: per-rule metrics use rule repository + alert list by rule.</summary>
public sealed class GetRuleMetricsHandlerTests
{
    private sealed class FakeRuleRepository : IRuleRepository
    {
        public Rule? Rule { get; init; }

        public Task AddAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Rule?.Id == id ? Rule : null);

        public Task UpdateAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Rule>>(Rule is null ? [] : [Rule]);

        public Task<PagedResult<Rule>> ListPagedAsync(
            RuleListQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAlertRepository : IAlertRepository
    {
        public IReadOnlyList<Alert> Alerts { get; init; } = [];

        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Alerts.FirstOrDefault(a => a.Id == id));

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PagedResult<Alert>> ListPagedAsync(
            AlertListQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Alerts);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Alerts.Where(a => a.RuleId == ruleId).ToList());
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_rule_missing()
    {
        var rules = new FakeRuleRepository { Rule = null };
        var alerts = new FakeAlertRepository();
        var handler = new GetRuleMetrics.Handler(rules, alerts);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_uses_current_rule_name()
    {
        var rid = Guid.NewGuid();
        var t0 = DateTime.UtcNow;
        var rule = new Rule(rid, "DisplayName", RuleTypes.SignalTypeEquals, "t", true, false, t0);
        var rules = new FakeRuleRepository { Rule = rule };
        var alerts = new FakeAlertRepository { Alerts = [] };
        var handler = new GetRuleMetrics.Handler(rules, alerts);

        var m = await handler.HandleAsync(rid, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Equal("DisplayName", m!.RuleName);
    }

    [Fact]
    public async Task HandleAsync_mixed_states_for_rule_only()
    {
        var rid = Guid.NewGuid();
        var otherRule = Guid.NewGuid();
        var t0 = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var rule = new Rule(rid, "R", RuleTypes.SignalTypeEquals, "x", true, false, t0);
        var alerts = new[]
        {
            new Alert(Guid.NewGuid(), Guid.NewGuid(), rid, t0),
            new Alert(Guid.NewGuid(), Guid.NewGuid(), rid, t0, true, t0.AddMinutes(1), "u"),
            new Alert(
                Guid.NewGuid(),
                Guid.NewGuid(),
                rid,
                t0,
                IsAcknowledged: false,
                AcknowledgedAtUtc: null,
                AcknowledgedByUserId: null,
                IsResolved: true,
                ResolvedAtUtc: t0.AddMinutes(3),
                ResolvedByUserId: "x"),
            new Alert(
                Guid.NewGuid(),
                Guid.NewGuid(),
                otherRule,
                t0,
                IsAcknowledged: false,
                AcknowledgedAtUtc: null,
                AcknowledgedByUserId: null,
                IsResolved: true,
                ResolvedAtUtc: t0.AddMinutes(1),
                ResolvedByUserId: "y")
        };

        var handler = new GetRuleMetrics.Handler(
            new FakeRuleRepository { Rule = rule },
            new FakeAlertRepository { Alerts = alerts });

        var m = await handler.HandleAsync(rid, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Equal(3, m!.TotalAlertsGenerated);
        Assert.Equal(1, m.OpenAlerts);
        Assert.Equal(1, m.AcknowledgedUnresolvedAlerts);
        Assert.Equal(1, m.ResolvedAlerts);
        Assert.Equal(0, m.ReopenedAlerts);
    }

    [Fact]
    public async Task HandleAsync_reopened_unresolved_counts_reopened_not_resolved()
    {
        var rid = Guid.NewGuid();
        var t0 = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var rule = new Rule(rid, "R2", RuleTypes.SignalTypeEquals, "y", true, false, t0);
        var a = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            rid,
            t0,
            IsAcknowledged: true,
            AcknowledgedAtUtc: t0.AddMinutes(1),
            AcknowledgedByUserId: "op",
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: t0.AddHours(2),
            ReopenedByUserId: "op");

        var handler = new GetRuleMetrics.Handler(
            new FakeRuleRepository { Rule = rule },
            new FakeAlertRepository { Alerts = [a] });

        var m = await handler.HandleAsync(rid, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Equal(1, m!.ReopenedAlerts);
        Assert.Equal(0, m.ResolvedAlerts);
        Assert.Null(m.AverageTimeToResolveSeconds);
    }
}
