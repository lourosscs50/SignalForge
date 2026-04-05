using SignalForge.Application;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Decisions;

public sealed class DecisionObservationRecorder(
    IDecisionRecordRepository repository,
    IDateTimeProvider clock) : IDecisionObservationRecorder
{
    private const int MaxSummaryLength = 1024;

    public async Task RecordAsync(
        AlertLifecycleEvent lifecycleEvent,
        Rule? rule,
        DecisionObservationContext? context,
        CancellationToken cancellationToken = default)
    {
        var record = BuildRecord(lifecycleEvent, rule, context, clock.UtcNow);
        await repository.AddAsync(record, cancellationToken);
    }

    private static DecisionRecord BuildRecord(
        AlertLifecycleEvent evt,
        Rule? rule,
        DecisionObservationContext? context,
        DateTime utcFallback)
    {
        var decisionType = MapDecisionType(evt.LifecycleTransitionType);
        var occurred = evt.OccurredAtUtc.UtcDateTime;
        if (occurred == default)
            occurred = utcFallback;

        var input = Truncate(
            context?.InputSummary ?? DefaultLifecycleInputSummary(evt));

        var output = Truncate(
            context?.OutputSummaryOverride ?? DefaultOutputSummary(evt.LifecycleTransitionType));

        var explanation = ExplanationFor(evt.LifecycleTransitionType);
        var recommended = RecommendedActionFor(evt.LifecycleTransitionType);

        var strategyKey = evt.LifecycleTransitionType == AlertLifecycleTransitionType.AlertCreated
            ? Truncate(context?.EvaluatorStrategyKey, 256)
            : evt.LifecycleTransitionType.ToString();

        var providerSummary = evt.LifecycleTransitionType == AlertLifecycleTransitionType.AlertCreated
            ? "SignalForge.BuiltinRulesEngine"
            : null;

        var (selectedOptionId, decisionOptions, chronoFlowExecutionInstanceId) =
            NormalizeAdditiveObservationFields(context);

        return new DecisionRecord(
            Id: Guid.NewGuid(),
            OccurredAtUtc: occurred,
            DecisionCategory: DecisionVisibilityKeys.CategoryAlertLifecycle,
            DecisionType: decisionType,
            Status: DecisionVisibilityKeys.StatusSucceeded,
            CorrelationId: evt.SignalId,
            ExecutionId: evt.AlertId,
            TraceId: null,
            AlertId: evt.AlertId,
            RuleId: evt.RuleId,
            SignalId: evt.SignalId,
            PolicyProfileKey: Truncate(rule?.Name ?? evt.RuleName, 256),
            StrategyPathKey: Truncate(strategyKey, 256),
            ProviderModelSummary: Truncate(providerSummary, 256),
            InputSummary: input,
            OutputSummary: output,
            ExplanationAvailable: true,
            ExplanationSummary: Truncate(explanation, MaxSummaryLength),
            ConfidenceBand: null,
            FallbackUsageCount: null,
            RetryUsageCount: null,
            RecommendedActionSummary: Truncate(recommended, MaxSummaryLength),
            AuditActorUserId: Truncate(ActorFrom(evt), 256),
            SelectedOptionId: selectedOptionId,
            DecisionOptions: decisionOptions,
            ChronoFlowExecutionInstanceId: chronoFlowExecutionInstanceId);
    }

    private const int MaxDecisionOptions = 64;
    private const int MaxOptionSummaryLength = 1024;
    private const int MaxSelectedOptionIdLength = 256;

    /// <summary>
    /// Copies explicit observation fields only — no correlation heuristics or defaults.
    /// </summary>
    private static (string? SelectedOptionId, IReadOnlyList<DecisionOptionSnapshot>? DecisionOptions, Guid? ChronoFlowExecutionInstanceId)
        NormalizeAdditiveObservationFields(DecisionObservationContext? context)
    {
        if (context is null)
            return (null, null, null);

        var chrono = context.ChronoFlowExecutionInstanceId;

        var selected = Truncate(context.SelectedOptionId?.Trim(), MaxSelectedOptionIdLength);
        if (selected is { Length: 0 })
            selected = null;

        if (context.DecisionOptions is null || context.DecisionOptions.Count == 0)
            return (selected, null, chrono);

        var ordered = context.DecisionOptions
            .OrderBy(o => o.Ordinal)
            .ThenBy(o => o.OptionId, StringComparer.Ordinal)
            .Take(MaxDecisionOptions)
            .Select(o => new DecisionOptionSnapshot(
                OptionId: Truncate(o.OptionId.Trim(), MaxSelectedOptionIdLength) ?? string.Empty,
                Summary: Truncate(o.Summary?.Trim(), MaxOptionSummaryLength),
                Ordinal: o.Ordinal))
            .Where(o => o.OptionId.Length > 0)
            .ToList();

        // Deterministic de-duplication by option id (first row wins after sort).
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<DecisionOptionSnapshot>(ordered.Count);
        foreach (var row in ordered)
        {
            if (!seen.Add(row.OptionId))
                continue;
            deduped.Add(row);
        }

        return (selected, deduped.Count == 0 ? null : deduped, chrono);
    }

    private static string MapDecisionType(AlertLifecycleTransitionType t) => t switch
    {
        AlertLifecycleTransitionType.AlertCreated => DecisionVisibilityKeys.Types.AlertCreated,
        AlertLifecycleTransitionType.AlertAcknowledged => DecisionVisibilityKeys.Types.AlertAcknowledged,
        AlertLifecycleTransitionType.AlertResolved => DecisionVisibilityKeys.Types.AlertResolved,
        AlertLifecycleTransitionType.AlertReopened => DecisionVisibilityKeys.Types.AlertReopened,
        _ => t.ToString()
    };

    private static string DefaultLifecycleInputSummary(AlertLifecycleEvent evt)
    {
        return
            $"lifecycleTransition={evt.LifecycleTransitionType};alertId={evt.AlertId};ruleId={evt.RuleId};signalId={evt.SignalId};status={evt.CurrentStatus}";
    }

    private static string DefaultOutputSummary(AlertLifecycleTransitionType t) => t switch
    {
        AlertLifecycleTransitionType.AlertCreated => "Alert opened after rule match.",
        AlertLifecycleTransitionType.AlertAcknowledged => "Alert acknowledgment recorded.",
        AlertLifecycleTransitionType.AlertResolved => "Alert resolution recorded.",
        AlertLifecycleTransitionType.AlertReopened => "Alert reopen recorded; resolution cleared.",
        _ => "Lifecycle transition recorded."
    };

    private static string ExplanationFor(AlertLifecycleTransitionType t) => t switch
    {
        AlertLifecycleTransitionType.AlertCreated =>
            "Structured operator summary: an active rule evaluated true for the ingested signal and produced a new alert. No model chain-of-thought is stored.",
        AlertLifecycleTransitionType.AlertAcknowledged =>
            "Structured operator summary: an authenticated operator acknowledged the alert.",
        AlertLifecycleTransitionType.AlertResolved =>
            "Structured operator summary: an authenticated operator resolved the alert.",
        AlertLifecycleTransitionType.AlertReopened =>
            "Structured operator summary: an authenticated operator reopened the alert, clearing resolution state.",
        _ => "Structured operator summary: alert lifecycle state changed."
    };

    private static string RecommendedActionFor(AlertLifecycleTransitionType t) => t switch
    {
        AlertLifecycleTransitionType.AlertCreated =>
            "Evaluate downstream automation hooks per platform policy; correlate using signal and alert identifiers.",
        AlertLifecycleTransitionType.AlertAcknowledged =>
            "Continue operational handling; automation may be requested per policy.",
        AlertLifecycleTransitionType.AlertResolved =>
            "Close operational loop or archive per runbooks; automation may be requested per policy.",
        AlertLifecycleTransitionType.AlertReopened =>
            "Resume active handling; automation may be requested per policy.",
        _ => "Review alert state and downstream orchestration policy."
    };

    private static string? ActorFrom(AlertLifecycleEvent evt) => evt.LifecycleTransitionType switch
    {
        AlertLifecycleTransitionType.AlertAcknowledged => evt.AcknowledgedByUserId,
        AlertLifecycleTransitionType.AlertResolved => evt.ResolvedByUserId,
        AlertLifecycleTransitionType.AlertReopened => evt.ReopenedByUserId,
        _ => null
    };

    private static string? Truncate(string? value, int max = MaxSummaryLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
