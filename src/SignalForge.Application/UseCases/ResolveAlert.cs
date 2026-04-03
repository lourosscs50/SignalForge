using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class ResolveAlert
{
    public sealed class Handler(IAlertRepository alerts, IDateTimeProvider clock, ICurrentUser currentUser)
    {
        public async Task<AlertResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var alert = await alerts.GetByIdAsync(id, cancellationToken);
            if (alert is null)
                return null;

            var actorUserId = currentUser.RequireUserId();

            var utcNow = clock.UtcNow;
            if (alert.IsResolved)
                return AlertMappings.ToAlertResponse(alert, utcNow);

            var updated = alert.Resolve(utcNow, actorUserId);
            await alerts.UpdateAsync(updated, cancellationToken);
            return AlertMappings.ToAlertResponse(updated, utcNow);
        }
    }
}
