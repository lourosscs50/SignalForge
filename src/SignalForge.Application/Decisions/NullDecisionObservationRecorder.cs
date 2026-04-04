using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Decisions;

public sealed class NullDecisionObservationRecorder : IDecisionObservationRecorder
{
    public static readonly NullDecisionObservationRecorder Instance = new();

    public Task RecordAsync(
        AlertLifecycleEvent lifecycleEvent,
        Rule? rule,
        DecisionObservationContext? context,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
