using SignalForge.Contracts.Automation;

namespace SignalForge.Application.Automation;

/// <summary>Default automation eligibility: created and reopened alerts request downstream attention; ack/resolve do not.</summary>
public sealed class DefaultAlertAutomationPolicy : IAlertAutomationPolicy
{
    public bool ShouldRequestAutomationTrigger(AlertLifecycleEvent lifecycleEvent) =>
        lifecycleEvent.LifecycleTransitionType is AlertLifecycleTransitionType.AlertCreated
            or AlertLifecycleTransitionType.AlertReopened;
}
