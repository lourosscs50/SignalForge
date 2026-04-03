using SignalForge.Application.Automation;
using SignalForge.Contracts.Automation;

namespace SignalForge.Application.Tests;

public sealed class DefaultAlertAutomationPolicyTests
{
    private static AlertLifecycleEvent Event(AlertLifecycleTransitionType t) =>
        new(
            t,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
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

    private readonly DefaultAlertAutomationPolicy _policy = new();

    [Theory]
    [InlineData(AlertLifecycleTransitionType.AlertCreated, true)]
    [InlineData(AlertLifecycleTransitionType.AlertReopened, true)]
    [InlineData(AlertLifecycleTransitionType.AlertAcknowledged, false)]
    [InlineData(AlertLifecycleTransitionType.AlertResolved, false)]
    public void ShouldRequestAutomationTrigger_matches_locked_default(
        AlertLifecycleTransitionType transition,
        bool expected)
    {
        Assert.Equal(expected, _policy.ShouldRequestAutomationTrigger(Event(transition)));
    }
}
