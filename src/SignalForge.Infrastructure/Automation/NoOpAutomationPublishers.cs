using SignalForge.Application;
using SignalForge.Contracts.Automation;

namespace SignalForge.Infrastructure.Automation;

public sealed class NoOpAlertLifecycleEventPublisher : IAlertLifecycleEventPublisher
{
    public Task PublishAsync(AlertLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class NoOpControlAutomationTriggerPublisher : IControlAutomationTriggerPublisher
{
    public Task PublishAsync(ControlAutomationTriggerRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
