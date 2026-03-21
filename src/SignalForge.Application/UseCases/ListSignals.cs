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
            var page = ListQueryNormalization.NormalizePage(query.Page);
            var pageSize = ListQueryNormalization.NormalizePageSize(query.PageSize);
            var normalized = new SignalListQuery(
                page,
                pageSize,
                string.IsNullOrWhiteSpace(query.Type) ? null : query.Type.Trim(),
                string.IsNullOrWhiteSpace(query.Source) ? null : query.Source.Trim(),
                query.FromOccurredUtc,
                query.ToOccurredUtc);

            var paged = await signals.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items
                .Select(s => new SignalResponse(
                    Id: s.Id,
                    Source: s.Source,
                    Type: s.Type,
                    TimestampUtc: new DateTimeOffset(s.OccurredAtUtc, TimeSpan.Zero),
                    Value: s.Value))
                .ToList();

            return new PagedResult<SignalResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}

