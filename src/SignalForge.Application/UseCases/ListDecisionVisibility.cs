using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Decisions;

namespace SignalForge.Application.UseCases;

public static class ListDecisionVisibility
{
    public sealed class Handler(IDecisionRecordRepository repository)
    {
        public async Task<PagedResult<DecisionVisibilityResponse>> HandleAsync(
            DecisionListQuery query,
            CancellationToken cancellationToken)
        {
            ListQueryNormalization.EnsureValidPaging(query.Page, query.PageSize);
            var page = await repository.ListPagedAsync(query, cancellationToken);
            return DecisionVisibilityMappings.ToVisibilityPage(page);
        }
    }
}
