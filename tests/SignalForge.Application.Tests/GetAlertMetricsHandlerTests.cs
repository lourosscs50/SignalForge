using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 5.3A: alert metrics summary query loads repository state and aggregates read-model metrics.</summary>
public sealed class GetAlertMetricsHandlerTests
{
    private sealed class ListAllFakeRepository : IAlertRepository
    {
        public IReadOnlyList<Alert> Alerts { get; init; } = [];

        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Alerts.FirstOrDefault(a => a.Id == id));

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken)
        {
            var ordered = Alerts.OrderByDescending(a => a.CreatedAtUtc).ToList();
            var total = ordered.Count;
            var items = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
            return Task.FromResult(new PagedResult<Alert>(items, query.Page, query.PageSize, total));
        }

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Alerts);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Alerts.Where(a => a.RuleId == ruleId).ToList());
    }

    [Fact]
    public async Task HandleAsync_returns_null_averages_when_no_durations_exist()
    {
        var t0 = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var onlyOpen = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0);
        var repo = new ListAllFakeRepository { Alerts = [onlyOpen] };
        var handler = new GetAlertMetrics.Handler(repo);

        var s = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(1, s.TotalAlerts);
        Assert.Null(s.AverageTimeToAcknowledgeSeconds);
        Assert.Null(s.AverageTimeToResolveSeconds);
    }

    [Fact]
    public async Task HandleAsync_average_ack_computes_from_created_to_acknowledged()
    {
        var t0 = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
        var a1 = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0, true, t0.AddSeconds(10), "u1");
        var a2 = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0, true, t0.AddSeconds(30), "u2");
        var repo = new ListAllFakeRepository { Alerts = [a1, a2] };
        var handler = new GetAlertMetrics.Handler(repo);

        var s = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(20.0, s.AverageTimeToAcknowledgeSeconds!.Value, precision: 5);
    }

    [Fact]
    public async Task HandleAsync_resolved_then_reopened_current_state_has_no_resolve_duration_but_still_reopened_count()
    {
        var t0 = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        // Current state: unresolved after reopen; has reopen marker; no ResolvedAtUtc
        var reopenedUnresolved = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            IsAcknowledged: true,
            AcknowledgedAtUtc: t0.AddMinutes(1),
            AcknowledgedByUserId: "op",
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: t0.AddHours(2),
            ReopenedByUserId: "op");

        var repo = new ListAllFakeRepository { Alerts = [reopenedUnresolved] };
        var handler = new GetAlertMetrics.Handler(repo);

        var s = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(1, s.ReopenedAlerts);
        Assert.Equal(0, s.ResolvedAlerts);
        Assert.Null(s.AverageTimeToResolveSeconds);
        Assert.Equal(60.0, s.AverageTimeToAcknowledgeSeconds!.Value, precision: 5);
    }
}
