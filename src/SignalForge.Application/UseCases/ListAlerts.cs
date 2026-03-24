using SignalForge.Application;
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
            ListQueryNormalization.EnsureValidPaging(query.Page, query.PageSize);
            var normalized = new AlertListQuery(
                query.Page,
                query.PageSize,
                query.RuleId,
                query.SignalId,
                query.FromCreatedUtc,
                query.ToCreatedUtc,
                query.IsAcknowledged);

            var paged = await alerts.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items.Select(AlertMappings.ToAlertResponse).ToList();

            return new PagedResult<AlertResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}
