using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Alerts;

namespace SignalForge.Application.UseCases;

public static class ListAlerts
{
    public sealed class Handler(IAlertRepository alerts)
    {
        public async Task<PagedResult<AlertResponse>> HandleAsync(AlertListQuery query, CancellationToken cancellationToken)
        {
            var page = ListQueryNormalization.NormalizePage(query.Page);
            var pageSize = ListQueryNormalization.NormalizePageSize(query.PageSize);
            var normalized = new AlertListQuery(
                page,
                pageSize,
                query.RuleId,
                query.SignalId,
                query.FromCreatedUtc,
                query.ToCreatedUtc);

            var paged = await alerts.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items
                .Select(a => new AlertResponse(
                    Id: a.Id,
                    SignalId: a.SignalId,
                    RuleId: a.RuleId,
                    CreatedAtUtc: new DateTimeOffset(a.CreatedAtUtc, TimeSpan.Zero)))
                .ToList();

            return new PagedResult<AlertResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}
