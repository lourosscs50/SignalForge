using SignalForge.Application;
using SignalForge.Contracts.Decisions;

namespace SignalForge.Application.UseCases;

public static class GetDecisionVisibilityMetrics
{
    public sealed class Handler(IDecisionRecordRepository repository)
    {
        public async Task<DecisionVisibilityMetricsResponse> HandleAsync(CancellationToken cancellationToken)
        {
            var rows = await repository.ListAllAsync(cancellationToken);
            return DecisionVisibilityMappings.ToMetricsResponse(rows);
        }
    }
}
