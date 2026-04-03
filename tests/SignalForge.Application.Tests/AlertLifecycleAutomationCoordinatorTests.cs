using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class AlertLifecycleAutomationCoordinatorTests
{
    [Fact]
    public async Task NotifyRealTransitionAsync_publishes_lifecycle_and_trigger_when_policy_allows()
    {
        var life = new CapturingLifecyclePublisher();
        var trigger = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trigger);
        var t = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var ruleId = Guid.NewGuid();
        var rule = new Rule(ruleId, "R", RuleTypes.SignalTypeEquals, "x", true, false, t);
        var alert = new Alert(Guid.NewGuid(), Guid.NewGuid(), ruleId, t);

        await coord.NotifyRealTransitionAsync(AlertLifecycleTransitionType.AlertCreated, alert, rule, t);

        var e = Assert.Single(life.Events);
        Assert.Equal(AlertLifecycleTransitionType.AlertCreated, e.LifecycleTransitionType);
        Assert.Equal(alert.Id, e.AlertId);
        Assert.Equal("R", e.RuleName);
        Assert.Single(trigger.Triggers);
    }

    [Fact]
    public async Task NotifyRealTransitionAsync_publishes_lifecycle_only_when_policy_denies_trigger()
    {
        var life = new CapturingLifecyclePublisher();
        var trigger = new CapturingTriggerPublisher();
        var alwaysDeny = new DenyAllAutomationPolicy();
        var coord = AutomationTestHarness.CreateCoordinator(life, trigger, alwaysDeny);
        var t = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var alert = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t);

        await coord.NotifyRealTransitionAsync(AlertLifecycleTransitionType.AlertCreated, alert, null, t);

        Assert.Single(life.Events);
        Assert.Empty(trigger.Triggers);
    }

    private sealed class DenyAllAutomationPolicy : IAlertAutomationPolicy
    {
        public bool ShouldRequestAutomationTrigger(AlertLifecycleEvent lifecycleEvent) => false;
    }
}
