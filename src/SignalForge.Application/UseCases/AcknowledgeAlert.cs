using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class AcknowledgeAlert
{
    public sealed class Handler(IAlertRepository alerts, IDateTimeProvider clock, ICurrentUser currentUser)
    {
        public async Task<AlertResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var alert = await alerts.GetByIdAsync(id, cancellationToken);
            if (alert is null)
                return null;

            var actorUserId = currentUser.RequireUserId();

            if (alert.IsAcknowledged)
                return AlertMappings.ToAlertResponse(alert);

            var updated = alert.Acknowledge(clock.UtcNow, actorUserId);
            await alerts.UpdateAsync(updated, cancellationToken);
            return AlertMappings.ToAlertResponse(updated);
        }
    }
}
