using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Contracts.Alerts;
using SignalForge.Contracts.Automation;

namespace SignalForge.Application.UseCases;

public static class ReopenAlert
{
    public sealed class Handler(
        IAlertRepository alerts,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        IRuleRepository rules,
        AlertLifecycleAutomationCoordinator lifecycleAutomation)
    {
        public async Task<AlertResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var alert = await alerts.GetByIdAsync(id, cancellationToken);
            if (alert is null)
                return null;

            var actorUserId = currentUser.RequireUserId();

            var utcNow = clock.UtcNow;
            if (!alert.IsResolved)
                return AlertMappings.ToAlertResponse(alert, utcNow);

            var updated = alert.Reopen(utcNow, actorUserId);
            await alerts.UpdateAsync(updated, cancellationToken);
            var rule = await rules.GetByIdAsync(updated.RuleId, cancellationToken);
            await lifecycleAutomation.NotifyRealTransitionAsync(
                AlertLifecycleTransitionType.AlertReopened,
                updated,
                rule,
                utcNow,
                cancellationToken);
            return AlertMappings.ToAlertResponse(updated, utcNow);
        }
    }
}
