using SignalForge.Application;
using SignalForge.Contracts.Decisions;

namespace SignalForge.Application.UseCases;

public static class GetDecisionVisibility
{
    public sealed class Handler(IDecisionRecordRepository repository)
    {
        public async Task<DecisionVisibilityResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var row = await repository.GetByIdAsync(id, cancellationToken);
            return row is null ? null : DecisionVisibilityMappings.ToVisibilityResponse(row);
        }
    }
}
