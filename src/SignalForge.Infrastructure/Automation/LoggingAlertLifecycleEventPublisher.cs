using Microsoft.Extensions.Logging;
using SignalForge.Application;
using SignalForge.Contracts.Automation;

namespace SignalForge.Infrastructure.Automation;

/// <summary>Structured logging observer for <see cref="AlertLifecycleEvent"/>; does not mutate or transport the event.</summary>
public sealed class LoggingAlertLifecycleEventPublisher(ILogger<LoggingAlertLifecycleEventPublisher> logger)
    : IAlertLifecycleEventPublisher
{
    public Task PublishAsync(AlertLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        logger.LogInformation(
            "Alert lifecycle event published: {LifecycleTransitionType} AlertId={AlertId} RuleId={RuleId} SignalId={SignalId} CurrentStatus={CurrentStatus} IsAcknowledged={IsAcknowledged} IsResolved={IsResolved} AcknowledgedAtUtc={AcknowledgedAtUtc} ResolvedAtUtc={ResolvedAtUtc} ReopenedAtUtc={ReopenedAtUtc} AcknowledgedByUserId={AcknowledgedByUserId} ResolvedByUserId={ResolvedByUserId} ReopenedByUserId={ReopenedByUserId} RuleName={RuleName}",
            lifecycleEvent.LifecycleTransitionType,
            lifecycleEvent.AlertId,
            lifecycleEvent.RuleId,
            lifecycleEvent.SignalId,
            lifecycleEvent.CurrentStatus,
            lifecycleEvent.IsAcknowledged,
            lifecycleEvent.IsResolved,
            lifecycleEvent.AcknowledgedAtUtc,
            lifecycleEvent.ResolvedAtUtc,
            lifecycleEvent.ReopenedAtUtc,
            lifecycleEvent.AcknowledgedByUserId,
            lifecycleEvent.ResolvedByUserId,
            lifecycleEvent.ReopenedByUserId,
            lifecycleEvent.RuleName);
        return Task.CompletedTask;
    }
}
