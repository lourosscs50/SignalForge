using Microsoft.Extensions.Logging;
using SignalForge.Application;
using SignalForge.Contracts.Automation;

namespace SignalForge.Infrastructure.Automation;

/// <summary>Structured logging observer for <see cref="ControlAutomationTriggerRequest"/>; does not alter downstream behavior.</summary>
public sealed class LoggingControlAutomationTriggerPublisher(ILogger<LoggingControlAutomationTriggerPublisher> logger)
    : IControlAutomationTriggerPublisher
{
    public Task PublishAsync(ControlAutomationTriggerRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        logger.LogInformation(
            "Control automation trigger published: {TriggerType} AlertId={AlertId} RuleId={RuleId} SignalId={SignalId} CurrentStatus={CurrentStatus} LifecycleEventType={LifecycleEventType} RuleName={RuleName} HasBeenReopened={HasBeenReopened} AcknowledgedByUserId={AcknowledgedByUserId} ResolvedByUserId={ResolvedByUserId} ReopenedByUserId={ReopenedByUserId}",
            request.TriggerType,
            request.AlertId,
            request.RuleId,
            request.SignalId,
            request.CurrentStatus,
            request.LifecycleEventType,
            request.RuleName,
            request.HasBeenReopened,
            request.AcknowledgedByUserId,
            request.ResolvedByUserId,
            request.ReopenedByUserId);
        return Task.CompletedTask;
    }
}
