using SignalForge.Contracts.Alerts;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class ListAlerts
{
    public sealed class Handler(IAlertRepository alerts)
    {
        public async Task<IReadOnlyList<AlertResponse>> HandleAsync(CancellationToken cancellationToken)
        {
            var list = await alerts.ListAsync(cancellationToken);

            return list
                .Select(a => new AlertResponse(
                    Id: a.Id,
                    SignalId: a.SignalId,
                    RuleId: a.RuleId,
                    CreatedAtUtc: new DateTimeOffset(a.CreatedAtUtc, TimeSpan.Zero)
                ))
                .ToList();
        }
    }
}
