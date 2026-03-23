using SignalForge.Contracts.Alerts;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class AlertMappings
{
    public static AlertResponse ToAlertResponse(Alert alert) =>
        new(
            alert.Id,
            alert.SignalId,
            alert.RuleId,
            new DateTimeOffset(alert.CreatedAtUtc, TimeSpan.Zero),
            alert.IsAcknowledged,
            alert.AcknowledgedAtUtc.HasValue
                ? new DateTimeOffset(alert.AcknowledgedAtUtc.Value, TimeSpan.Zero)
                : null);
}
