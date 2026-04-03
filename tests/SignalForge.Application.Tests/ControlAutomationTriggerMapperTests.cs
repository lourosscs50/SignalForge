using SignalForge.Application.Automation;
using SignalForge.Contracts.Automation;

namespace SignalForge.Application.Tests;

public sealed class ControlAutomationTriggerMapperTests
{
    [Fact]
    public void ToTriggerRequest_maps_lifecycle_fields_and_no_workflow_ids()
    {
        var ruleId = Guid.NewGuid();
        var ackAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var evt = new AlertLifecycleEvent(
            AlertLifecycleTransitionType.AlertAcknowledged,
            Guid.NewGuid(),
            ruleId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 3, 1, 10, 5, 0, TimeSpan.Zero),
            "Acknowledged",
            true,
            false,
            ackAt,
            null,
            null,
            "u1",
            null,
            null,
            "Rule-X");

        var t = ControlAutomationTriggerMapper.ToTriggerRequest(evt);

        Assert.Equal(nameof(AlertLifecycleTransitionType.AlertAcknowledged), t.TriggerType);
        Assert.Equal(nameof(AlertLifecycleTransitionType.AlertAcknowledged), t.LifecycleEventType);
        Assert.Equal(evt.AlertId, t.AlertId);
        Assert.Equal(evt.RuleId, t.RuleId);
        Assert.Equal(evt.SignalId, t.SignalId);
        Assert.Equal(evt.OccurredAtUtc, t.OccurredAtUtc);
        Assert.Equal("Acknowledged", t.CurrentStatus);
        Assert.Equal("u1", t.AcknowledgedByUserId);
        Assert.Equal("Rule-X", t.RuleName);
        Assert.False(t.HasBeenReopened);
    }
}
