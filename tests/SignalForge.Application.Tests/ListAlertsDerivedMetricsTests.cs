using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 5.3A: paged list uses the same alert read mapping as detail.</summary>
public sealed class ListAlertsDerivedMetricsTests
{
    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class AlertsOnlyRepository : IAlertRepository
    {
        public required IReadOnlyList<Alert> Alerts { get; init; }

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
    public async Task ListAlerts_mapped_items_include_TimeToAcknowledgeSeconds_and_HasBeenReopened()
    {
        var t0 = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var alert = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            IsAcknowledged: true,
            AcknowledgedAtUtc: t0.AddSeconds(45),
            AcknowledgedByUserId: "u",
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: t0.AddHours(1),
            ReopenedByUserId: "u");

        var readAt = t0.AddMinutes(10);
        var repo = new AlertsOnlyRepository { Alerts = [alert] };
        var handler = new ListAlerts.Handler(repo, new FixedClock(readAt));
        var query = new AlertListQuery(1, 10, null, null, null, null, null, null);

        var page = await handler.HandleAsync(query, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(45.0, item.TimeToAcknowledgeSeconds!.Value, precision: 5);
        Assert.Null(item.TimeToResolveSeconds);
        Assert.True(item.HasBeenReopened);
        Assert.Equal(AlertMappings.StatusAcknowledged, item.CurrentStatus);
        Assert.Equal(600.0, item.AgeSeconds, precision: 5);
    }
}
