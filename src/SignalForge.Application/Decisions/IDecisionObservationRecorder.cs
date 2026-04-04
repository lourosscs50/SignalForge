using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Decisions;

public interface IDecisionObservationRecorder
{
    Task RecordAsync(
        AlertLifecycleEvent lifecycleEvent,
        Rule? rule,
        DecisionObservationContext? context,
        CancellationToken cancellationToken = default);
}
