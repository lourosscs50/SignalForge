using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class ResolveAlert
{
    public sealed class Handler(IAlertRepository alerts, IDateTimeProvider clock)
    {
        public async Task<AlertResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var alert = await alerts.GetByIdAsync(id, cancellationToken);
            if (alert is null)
                return null;

            if (alert.IsResolved)
                return AlertMappings.ToAlertResponse(alert);

            var updated = alert.Resolve(clock.UtcNow);
            await alerts.UpdateAsync(updated, cancellationToken);
            return AlertMappings.ToAlertResponse(updated);
        }
    }
}
