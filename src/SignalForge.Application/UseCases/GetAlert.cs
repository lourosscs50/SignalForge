using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class GetAlert
{
    public sealed class Handler(
        IAlertRepository alerts,
        IRuleRepository rules,
        ISignalRepository signals)
    {
        public async Task<AlertDetailResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var alert = await alerts.GetByIdAsync(id, cancellationToken);
            if (alert is null)
                return null;

            var rule = await rules.GetByIdAsync(alert.RuleId, cancellationToken);
            var signal = await signals.GetByIdAsync(alert.SignalId, cancellationToken);
            if (rule is null || signal is null)
                return null;

            return new AlertDetailResponse(
                Id: alert.Id,
                SignalId: alert.SignalId,
                RuleId: alert.RuleId,
                CreatedAtUtc: new DateTimeOffset(alert.CreatedAtUtc, TimeSpan.Zero),
                IsAcknowledged: alert.IsAcknowledged,
                AcknowledgedAtUtc: alert.AcknowledgedAtUtc.HasValue
                    ? new DateTimeOffset(alert.AcknowledgedAtUtc.Value, TimeSpan.Zero)
                    : null,
                AcknowledgedByUserId: alert.AcknowledgedByUserId,
                IsResolved: alert.IsResolved,
                ResolvedAtUtc: alert.ResolvedAtUtc.HasValue
                    ? new DateTimeOffset(alert.ResolvedAtUtc.Value, TimeSpan.Zero)
                    : null,
                ResolvedByUserId: alert.ResolvedByUserId,
                ReopenedAtUtc: alert.ReopenedAtUtc.HasValue
                    ? new DateTimeOffset(alert.ReopenedAtUtc.Value, TimeSpan.Zero)
                    : null,
                ReopenedByUserId: alert.ReopenedByUserId,
                Rule: AlertDetailMappings.ToAlertRuleSummary(rule),
                Signal: AlertDetailMappings.ToAlertSignalSummary(signal));
        }
    }
}
