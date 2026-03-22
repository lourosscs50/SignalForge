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
                Rule: new AlertRuleSummary(
                    rule.Id,
                    rule.Name,
                    rule.RuleType,
                    rule.IsActive),
                Signal: new AlertSignalSummary(
                    signal.Id,
                    signal.Source,
                    signal.Type,
                    new DateTimeOffset(signal.OccurredAtUtc, TimeSpan.Zero)));
        }
    }
}
