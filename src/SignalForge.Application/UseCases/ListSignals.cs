using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Signals;

namespace SignalForge.Application.UseCases;

public static class ListSignals
{
    public sealed class Handler(ISignalRepository signals)
    {
        public async Task<PagedResult<SignalResponse>> HandleAsync(SignalListQuery query, CancellationToken cancellationToken)
        {
            ListQueryNormalization.EnsureValidPaging(query.Page, query.PageSize);
            var normalized = new SignalListQuery(
                query.Page,
                query.PageSize,
                string.IsNullOrWhiteSpace(query.Type) ? null : query.Type.Trim(),
                string.IsNullOrWhiteSpace(query.Source) ? null : query.Source.Trim(),
                query.FromOccurredUtc,
                query.ToOccurredUtc);

            var paged = await signals.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items.Select(SignalMappings.ToResponse).ToList();

            return new PagedResult<SignalResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}

