using SignalForge.Application;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class GetAlertMetrics
{
    public sealed class Handler(IAlertRepository alerts)
    {
        public async Task<AlertMetricsSummaryResponse> HandleAsync(CancellationToken cancellationToken)
        {
            var items = await alerts.ListAllAsync(cancellationToken);
            return AlertMappings.ToMetricsSummary(items);
        }
    }
}
