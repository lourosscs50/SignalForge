using SignalForge.Application.Decisions;
using SignalForge.Contracts;
using SignalForge.Contracts.Decisions;
using SignalForge.Domain;

namespace SignalForge.Application;

public static class DecisionVisibilityMappings
{
    public static DecisionVisibilityResponse ToVisibilityResponse(DecisionRecord d)
    {
        var input = new DecisionInputSummary(d.InputSummary ?? string.Empty);
        var output = new DecisionOutputSummary(d.OutputSummary ?? string.Empty);
        var explanation = new DecisionExplanationSummary(
            d.ExplanationAvailable,
            d.ExplanationSummary,
            ReasonCodesFor(d.DecisionType),
            d.ConfidenceBand,
            d.FallbackUsageCount,
            d.RetryUsageCount);

        var related = BuildRelatedIds(d);
        var trace = new DecisionTraceSummary(
            CorrelationId: d.CorrelationId,
            ExecutionId: d.ExecutionId,
            TraceId: d.TraceId,
            RelatedEntityIds: related,
            SignalEntityId: d.SignalId,
            AlertEntityId: d.AlertId,
            ChronoFlowExecutionInstanceId: null);

        return new DecisionVisibilityResponse(
            DecisionId: d.Id,
            DecisionCategory: d.DecisionCategory,
            DecisionType: d.DecisionType,
            OccurredAtUtc: new DateTimeOffset(DateTime.SpecifyKind(d.OccurredAtUtc, DateTimeKind.Utc)),
            Status: d.Status,
            Input: input,
            Output: output,
            PolicyProfileKey: d.PolicyProfileKey,
            StrategyPathKey: d.StrategyPathKey,
            ProviderModelSummary: d.ProviderModelSummary,
            Explanation: explanation,
            RecommendedDownstreamSummary: d.RecommendedActionSummary,
            AuditActorUserId: d.AuditActorUserId,
            Trace: trace);
    }

    public static PagedResult<DecisionVisibilityResponse> ToVisibilityPage(PagedResult<DecisionRecord> page)
    {
        var items = page.Items.Select(ToVisibilityResponse).ToList();
        return new PagedResult<DecisionVisibilityResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }

    public static DecisionVisibilityMetricsResponse ToMetricsResponse(IReadOnlyList<DecisionRecord> records)
    {
        var counts = records
            .GroupBy(r => r.DecisionType)
            .Select(g => new DecisionTypeCountRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.DecisionType)
            .ToList();

        return new DecisionVisibilityMetricsResponse(records.Count, counts);
    }

    private static IReadOnlyList<Guid> BuildRelatedIds(DecisionRecord d)
    {
        var list = new List<Guid>(3);
        if (d.AlertId.HasValue)
            list.Add(d.AlertId.Value);
        if (d.RuleId.HasValue)
            list.Add(d.RuleId.Value);
        if (d.SignalId.HasValue)
            list.Add(d.SignalId.Value);
        return list;
    }

    private static IReadOnlyList<string>? ReasonCodesFor(string decisionType) => decisionType switch
    {
        DecisionVisibilityKeys.Types.AlertCreated => ["SF.RULE.MATCH", "SF.ALERT.OPENED"],
        DecisionVisibilityKeys.Types.AlertAcknowledged => ["SF.ALERT.ACK"],
        DecisionVisibilityKeys.Types.AlertResolved => ["SF.ALERT.RESOLVE"],
        DecisionVisibilityKeys.Types.AlertReopened => ["SF.ALERT.REOPEN"],
        _ => null
    };
}
