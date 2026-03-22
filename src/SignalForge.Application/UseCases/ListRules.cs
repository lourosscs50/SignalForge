using SignalForge.Application;
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
            ListQueryNormalization.EnsureValidPaging(query.Page, query.PageSize);
            var normalized = new RuleListQuery(
                query.Page,
                query.PageSize,
                query.IsActive,
                string.IsNullOrWhiteSpace(query.RuleType) ? null : query.RuleType.Trim());

            var paged = await rules.ListPagedAsync(normalized, cancellationToken);

            var items = paged.Items.Select(RuleMappings.ToResponse).ToList();

            return new PagedResult<RuleResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
        }
    }
}
