using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Rules;

namespace SignalForge.Application.UseCases;

public static class ListRules
{
    public sealed class Handler(IRuleRepository rules)
    {
        public async Task<PagedResult<RuleResponse>> HandleAsync(RuleListQuery query, CancellationToken cancellationToken)
        {
            var page = ListQueryNormalization.NormalizePage(query.Page);
            var pageSize = ListQueryNormalization.NormalizePageSize(query.PageSize);
            var normalized = new RuleListQuery(
                page,
                pageSize,
                query.IsActive,
                string.IsNullOrWhiteSpace(query.RuleType) ? null : query.RuleType.Trim());

            var paged = await rules.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items
                .Select(r => new RuleResponse(
                    Id: r.Id,
                    Name: r.Name,
                    RuleType: r.RuleType,
                    MatchValue: r.MatchValue,
                    IsActive: r.IsActive,
                    CreatedAtUtc: new DateTimeOffset(r.CreatedAtUtc, TimeSpan.Zero)))
                .ToList();

            return new PagedResult<RuleResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}
