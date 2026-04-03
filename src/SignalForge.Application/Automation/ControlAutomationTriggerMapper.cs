using SignalForge.Contracts.Automation;

namespace SignalForge.Application.Automation;

internal static class ControlAutomationTriggerMapper
{
    public static ControlAutomationTriggerRequest ToTriggerRequest(AlertLifecycleEvent e)
    {
        var typeName = e.LifecycleTransitionType.ToString();
        return new ControlAutomationTriggerRequest(
            TriggerType: typeName,
            AlertId: e.AlertId,
            RuleId: e.RuleId,
            SignalId: e.SignalId,
            OccurredAtUtc: e.OccurredAtUtc,
            CurrentStatus: e.CurrentStatus,
            LifecycleEventType: typeName,
            AcknowledgedByUserId: e.AcknowledgedByUserId,
            ResolvedByUserId: e.ResolvedByUserId,
            ReopenedByUserId: e.ReopenedByUserId,
            RuleName: e.RuleName,
            HasBeenReopened: e.ReopenedAtUtc.HasValue);
    }
}
