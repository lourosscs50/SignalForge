using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Automation;

/// <summary>Coordinates lifecycle event publication and optional automation trigger requests after real transitions only.</summary>
public sealed class AlertLifecycleAutomationCoordinator(
    IAlertLifecycleEventPublisher lifecyclePublisher,
    IAlertAutomationPolicy automationPolicy,
    IControlAutomationTriggerPublisher triggerPublisher)
{
    public async Task NotifyRealTransitionAsync(
        AlertLifecycleTransitionType transitionType,
        Alert alert,
        Rule? rule,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        var evt = BuildEvent(transitionType, alert, rule, occurredAtUtc);
        await lifecyclePublisher.PublishAsync(evt, cancellationToken);

        if (automationPolicy.ShouldRequestAutomationTrigger(evt))
        {
            var trigger = ControlAutomationTriggerMapper.ToTriggerRequest(evt);
            await triggerPublisher.PublishAsync(trigger, cancellationToken);
        }
    }

    private static AlertLifecycleEvent BuildEvent(
        AlertLifecycleTransitionType transitionType,
        Alert alert,
        Rule? rule,
        DateTime occurredAtUtc)
    {
        return new AlertLifecycleEvent(
            LifecycleTransitionType: transitionType,
            AlertId: alert.Id,
            RuleId: alert.RuleId,
            SignalId: alert.SignalId,
            OccurredAtUtc: new DateTimeOffset(occurredAtUtc, TimeSpan.Zero),
            CurrentStatus: AlertMappings.CurrentStatus(alert),
            IsAcknowledged: alert.IsAcknowledged,
            IsResolved: alert.IsResolved,
            AcknowledgedAtUtc: ToOffset(alert.AcknowledgedAtUtc),
            ResolvedAtUtc: ToOffset(alert.ResolvedAtUtc),
            ReopenedAtUtc: ToOffset(alert.ReopenedAtUtc),
            AcknowledgedByUserId: alert.AcknowledgedByUserId,
            ResolvedByUserId: alert.ResolvedByUserId,
            ReopenedByUserId: alert.ReopenedByUserId,
            RuleName: rule?.Name);
    }

    private static DateTimeOffset? ToOffset(DateTime? utc) =>
        utc.HasValue ? new DateTimeOffset(utc.Value, TimeSpan.Zero) : null;
}
