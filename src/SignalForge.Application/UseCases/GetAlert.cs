using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class GetAlert
{
    public sealed class Handler(
        IAlertRepository alerts,
        IRuleRepository rules,
        ISignalRepository signals,
        IDateTimeProvider clock)
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

            return AlertMappings.ToAlertDetailResponse(
                alert,
                AlertDetailMappings.ToAlertRuleSummary(rule),
                AlertDetailMappings.ToAlertSignalSummary(signal),
                clock.UtcNow);
        }
    }
}
