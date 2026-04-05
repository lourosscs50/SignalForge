using SignalForge.Application;
using SignalForge.Application.Decisions;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Contracts.Decisions;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class DecisionVisibilityApplicationTests
{
    private sealed class FakeDecisionRecordRepository : IDecisionRecordRepository
    {
        private readonly List<DecisionRecord> _items = [];

        public IReadOnlyList<DecisionRecord> Items => _items;

        public Task AddAsync(DecisionRecord record, CancellationToken cancellationToken)
        {
            _items.Add(record);
            return Task.CompletedTask;
        }

        public Task<DecisionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<PagedResult<DecisionRecord>> ListPagedAsync(DecisionListQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<DecisionRecord> q = _items;
            if (!string.IsNullOrWhiteSpace(query.DecisionType))
            {
                var t = query.DecisionType.Trim();
                q = q.Where(d => d.DecisionType == t);
            }

            if (query.CorrelationId.HasValue)
                q = q.Where(d => d.CorrelationId == query.CorrelationId.Value);

            var list = q.OrderByDescending(d => d.OccurredAtUtc).ToList();
            var total = list.Count;
            var pageItems = list
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
            return Task.FromResult(new PagedResult<DecisionRecord>(pageItems, query.Page, query.PageSize, total));
        }

        public Task<IReadOnlyList<DecisionRecord>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DecisionRecord>>(_items.OrderByDescending(d => d.OccurredAtUtc).ToList());
    }

    private static DecisionRecord Sample(Guid id, Guid signalId, Guid alertId) =>
        new(
            Id: id,
            OccurredAtUtc: new DateTime(2026, 4, 4, 12, 0, 0, DateTimeKind.Utc),
            DecisionCategory: DecisionVisibilityKeys.CategoryAlertLifecycle,
            DecisionType: DecisionVisibilityKeys.Types.AlertCreated,
            Status: DecisionVisibilityKeys.StatusSucceeded,
            CorrelationId: signalId,
            ExecutionId: alertId,
            TraceId: null,
            AlertId: alertId,
            RuleId: Guid.NewGuid(),
            SignalId: signalId,
            PolicyProfileKey: "Rule-A",
            StrategyPathKey: "SignalTypeEqualsRuleEvaluator",
            ProviderModelSummary: "SignalForge.BuiltinRulesEngine",
            InputSummary: "signalId=...",
            OutputSummary: "Alert opened after rule match.",
            ExplanationAvailable: true,
            ExplanationSummary: "Structured operator summary.",
            ConfidenceBand: null,
            FallbackUsageCount: null,
            RetryUsageCount: null,
            RecommendedActionSummary: "Evaluate downstream automation hooks per platform policy.",
            AuditActorUserId: null);

    [Fact]
    public async Task GetDecisionVisibility_returns_mapped_row_when_present()
    {
        var id = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var repo = new FakeDecisionRecordRepository();
        await repo.AddAsync(Sample(id, signalId, alertId), CancellationToken.None);

        var handler = new GetDecisionVisibility.Handler(repo);
        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.DecisionId);
        Assert.Equal(signalId, result.Trace.CorrelationId);
        Assert.Equal(alertId, result.Trace.ExecutionId);
        Assert.Equal(signalId, result.Trace.SignalEntityId);
        Assert.Equal(alertId, result.Trace.AlertEntityId);
        Assert.Null(result.Trace.ChronoFlowExecutionInstanceId);
        Assert.Null(result.SelectedOptionId);
        Assert.Null(result.DecisionOptions);
        Assert.Contains(alertId, result.Trace.RelatedEntityIds);
        Assert.True(result.Explanation.ExplanationAvailable);
        Assert.NotNull(result.Explanation.ReasonCodes);
        Assert.Contains("SF.RULE.MATCH", result.Explanation.ReasonCodes);
    }

    [Fact]
    public async Task GetDecisionVisibility_returns_null_when_missing()
    {
        var handler = new GetDecisionVisibility.Handler(new FakeDecisionRecordRepository());
        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task ListDecisionVisibility_filters_by_decision_type_and_correlation()
    {
        var repo = new FakeDecisionRecordRepository();
        var s1 = Guid.NewGuid();
        var a1 = Guid.NewGuid();
        await repo.AddAsync(Sample(Guid.NewGuid(), s1, a1), CancellationToken.None);
        await repo.AddAsync(
            Sample(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) with
            {
                DecisionType = DecisionVisibilityKeys.Types.AlertAcknowledged,
                AuditActorUserId = "actor-1"
            },
            CancellationToken.None);

        var handler = new ListDecisionVisibility.Handler(repo);
        var page = await handler.HandleAsync(
            new DecisionListQuery(1, 20, null, DecisionVisibilityKeys.Types.AlertCreated, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal(DecisionVisibilityKeys.Types.AlertCreated, page.Items[0].DecisionType);

        var byCorr = await handler.HandleAsync(
            new DecisionListQuery(1, 20, null, null, null, null, null, s1, null, null, null, null),
            CancellationToken.None);
        Assert.Single(byCorr.Items);
        Assert.Equal(s1, byCorr.Items[0].Trace.CorrelationId);
    }

    [Fact]
    public void ToVisibilityResponse_maps_selected_option_decision_options_and_chrono_when_on_record()
    {
        var id = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var chrono = Guid.NewGuid();
        var record = Sample(id, signalId, alertId) with
        {
            SelectedOptionId = "option-b",
            DecisionOptions =
            [
                new DecisionOptionSnapshot("option-a", "First", 0),
                new DecisionOptionSnapshot("option-b", "Second", 1),
            ],
            ChronoFlowExecutionInstanceId = chrono,
        };

        var mapped = DecisionVisibilityMappings.ToVisibilityResponse(record);

        Assert.Equal("option-b", mapped.SelectedOptionId);
        Assert.NotNull(mapped.DecisionOptions);
        Assert.Equal(2, mapped.DecisionOptions.Count);
        Assert.Equal("option-a", mapped.DecisionOptions[0].OptionId);
        Assert.Equal(0, mapped.DecisionOptions[0].Ordinal);
        Assert.Equal("option-b", mapped.DecisionOptions[1].OptionId);
        Assert.Equal(chrono, mapped.Trace.ChronoFlowExecutionInstanceId);
    }

    [Fact]
    public void ToVisibilityResponse_maps_empty_selected_to_null_and_skips_empty_option_lists()
    {
        var id = Guid.NewGuid();
        var record = Sample(id, Guid.NewGuid(), Guid.NewGuid()) with
        {
            SelectedOptionId = "   ",
            DecisionOptions = [],
        };

        var mapped = DecisionVisibilityMappings.ToVisibilityResponse(record);

        Assert.Null(mapped.SelectedOptionId);
        Assert.Null(mapped.DecisionOptions);
    }

    [Fact]
    public async Task GetDecisionVisibilityMetrics_groups_by_type()
    {
        var repo = new FakeDecisionRecordRepository();
        await repo.AddAsync(Sample(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await repo.AddAsync(
            Sample(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) with { DecisionType = DecisionVisibilityKeys.Types.AlertAcknowledged },
            CancellationToken.None);

        var handler = new GetDecisionVisibilityMetrics.Handler(repo);
        var m = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(2, m.TotalDecisions);
        Assert.Equal(2, m.CountsByDecisionType.Count);
    }
}
