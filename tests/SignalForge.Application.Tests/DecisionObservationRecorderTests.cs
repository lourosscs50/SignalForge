using SignalForge.Application;
using SignalForge.Application.Decisions;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class DecisionObservationRecorderTests
{
    private sealed class CapturingDecisionRepo : IDecisionRecordRepository
    {
        public DecisionRecord? Last { get; private set; }

        public Task AddAsync(DecisionRecord record, CancellationToken cancellationToken)
        {
            Last = record;
            return Task.CompletedTask;
        }

        public Task<DecisionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DecisionRecord?>(null);

        public Task<PagedResult<DecisionRecord>> ListPagedAsync(
            DecisionListQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<DecisionRecord>([], query.Page, query.PageSize, 0));

        public Task<IReadOnlyList<DecisionRecord>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DecisionRecord>>([]);
    }

    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    [Fact]
    public async Task RecordAsync_persists_safe_summaries_and_trace_fields_for_rule_match_context()
    {
        var repo = new CapturingDecisionRepo();
        var clock = new FixedClock(new DateTime(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new DecisionObservationRecorder(repo, clock);
        var ruleId = Guid.NewGuid();
        var rule = new Rule(ruleId, "MyRule", RuleTypes.SignalTypeEquals, "t", true, false, clock.UtcNow);
        var alertId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alert = new Alert(alertId, signalId, ruleId, clock.UtcNow);

        var evt = new AlertLifecycleEvent(
            AlertLifecycleTransitionType.AlertCreated,
            alertId,
            ruleId,
            signalId,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            "Open",
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            rule.Name);

        await recorder.RecordAsync(
            evt,
            rule,
            new DecisionObservationContext(
                EvaluatorStrategyKey: "SignalTypeEqualsRuleEvaluator",
                InputSummary: new string('x', 2000)),
            CancellationToken.None);

        Assert.NotNull(repo.Last);
        Assert.Equal(DecisionVisibilityKeys.Types.AlertCreated, repo.Last!.DecisionType);
        Assert.Equal(signalId, repo.Last.CorrelationId);
        Assert.Equal(alertId, repo.Last.ExecutionId);
        Assert.Equal("MyRule", repo.Last.PolicyProfileKey);
        Assert.Equal("SignalForge.BuiltinRulesEngine", repo.Last.ProviderModelSummary);
        Assert.True(repo.Last.ExplanationAvailable);
        Assert.DoesNotContain("prompt", repo.Last.ExplanationSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1024, repo.Last.InputSummary!.Length);
        Assert.Null(repo.Last.SelectedOptionId);
        Assert.Null(repo.Last.DecisionOptions);
        Assert.Null(repo.Last.ChronoFlowExecutionInstanceId);
    }

    [Fact]
    public async Task RecordAsync_maps_actor_for_ack_transition()
    {
        var repo = new CapturingDecisionRepo();
        var clock = new FixedClock(new DateTime(2026, 4, 4, 1, 0, 0, DateTimeKind.Utc));
        var recorder = new DecisionObservationRecorder(repo, clock);
        var ruleId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var alert = new Alert(alertId, signalId, ruleId, clock.UtcNow, true, clock.UtcNow, "user-42");

        var evt = new AlertLifecycleEvent(
            AlertLifecycleTransitionType.AlertAcknowledged,
            alertId,
            ruleId,
            signalId,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            "Acknowledged",
            true,
            false,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            null,
            null,
            "user-42",
            null,
            null,
            "R");

        await recorder.RecordAsync(evt, null, null, CancellationToken.None);

        Assert.Equal("user-42", repo.Last!.AuditActorUserId);
        Assert.Equal(DecisionVisibilityKeys.Types.AlertAcknowledged, repo.Last.DecisionType);
        Assert.Null(repo.Last.SelectedOptionId);
        Assert.Null(repo.Last.DecisionOptions);
        Assert.Null(repo.Last.ChronoFlowExecutionInstanceId);
    }

    [Fact]
    public async Task RecordAsync_persists_explicit_context_options_selected_option_and_chrono_id_only()
    {
        var repo = new CapturingDecisionRepo();
        var clock = new FixedClock(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new DecisionObservationRecorder(repo, clock);
        var ruleId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var chronoId = Guid.NewGuid();

        var evt = new AlertLifecycleEvent(
            AlertLifecycleTransitionType.AlertCreated,
            alertId,
            ruleId,
            signalId,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            "Open",
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            "R");

        await recorder.RecordAsync(
            evt,
            null,
            new DecisionObservationContext(
                SelectedOptionId: "opt-b",
                DecisionOptions:
                [
                    new DecisionOptionSnapshot("opt-b", "B wins", 1),
                    new DecisionOptionSnapshot("opt-a", "A", 0),
                    new DecisionOptionSnapshot("opt-b", "duplicate ignored", 2),
                ],
                ChronoFlowExecutionInstanceId: chronoId),
            CancellationToken.None);

        Assert.NotNull(repo.Last);
        Assert.Equal("opt-b", repo.Last!.SelectedOptionId);
        Assert.Equal(chronoId, repo.Last.ChronoFlowExecutionInstanceId);
        Assert.NotNull(repo.Last.DecisionOptions);
        Assert.Equal(2, repo.Last.DecisionOptions.Count);
        Assert.Equal("opt-a", repo.Last.DecisionOptions[0].OptionId);
        Assert.Equal("opt-b", repo.Last.DecisionOptions[1].OptionId);
        Assert.Equal("B wins", repo.Last.DecisionOptions[1].Summary);
    }
}
