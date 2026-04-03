using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 5.3C: GET alert detail uses clock for AgeSeconds alongside list mapping.</summary>
public sealed class GetAlertTriageHandlerTests
{
    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class SingleAlertRepo : IAlertRepository
    {
        public required Alert Alert { get; init; }

        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Alert.Id == id ? Alert : null);

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken)
        {
            var items = new List<Alert> { Alert };
            if (query.RuleId is { } rid && rid != Alert.RuleId)
                items = [];
            return Task.FromResult(
                new PagedResult<Alert>(items, query.Page, query.PageSize, items.Count));
        }

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>([Alert]);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Alert.RuleId == ruleId ? [Alert] : []);
    }

    private sealed class SingleRuleRepo : IRuleRepository
    {
        public required Rule Rule { get; init; }

        public Task AddAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Rule.Id == id ? Rule : null);

        public Task UpdateAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Rule>>([Rule]);

        public Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class SingleSignalRepo : ISignalRepository
    {
        public required Signal Signal { get; init; }

        public Task AddAsync(Signal signal, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Signal?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Signal.Id == id ? Signal : null);

        public Task<PagedResult<Signal>> ListPagedAsync(SignalListQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Detail_matches_list_triage_fields_for_same_clock_instant()
    {
        var t0 = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        var readAt = t0.AddMinutes(30);
        var ruleId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alertId = Guid.NewGuid();

        var alert = new Alert(alertId, signalId, ruleId, t0, true, t0.AddMinutes(1), "u");
        var rule = new Rule(ruleId, "Nm", RuleTypes.SignalTypeEquals, "t", true, false, t0);
        var signal = new Signal(signalId, "src", "t", "", 1.0, t0, t0);

        var clock = new FixedClock(readAt);
        var listHandler = new ListAlerts.Handler(new SingleAlertRepo { Alert = alert }, clock);
        var getHandler = new GetAlert.Handler(
            new SingleAlertRepo { Alert = alert },
            new SingleRuleRepo { Rule = rule },
            new SingleSignalRepo { Signal = signal },
            clock);

        var page = await listHandler.HandleAsync(
            new AlertListQuery(1, 10, null, null, null, null, null, null),
            CancellationToken.None);
        var detail = await getHandler.HandleAsync(alertId, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.NotNull(detail);
        Assert.Equal(item.CurrentStatus, detail!.CurrentStatus);
        Assert.Equal(item.AgeSeconds, detail.AgeSeconds);
        Assert.Equal(AlertMappings.StatusAcknowledged, detail.CurrentStatus);
        Assert.Equal(1800.0, detail.AgeSeconds, precision: 5);
    }
}
