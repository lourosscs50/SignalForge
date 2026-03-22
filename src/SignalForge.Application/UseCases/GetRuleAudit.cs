using SignalForge.Application;
using SignalForge.Contracts.Rules;

namespace SignalForge.Application.UseCases;

public static class GetRuleAudit
{
    public sealed class Handler(IRuleRepository rules, IRuleAuditRepository audit)
    {
        public async Task<IReadOnlyList<RuleAuditEntryResponse>?> HandleAsync(
            Guid ruleId,
            CancellationToken cancellationToken)
        {
            if (await rules.GetByIdAsync(ruleId, cancellationToken) is null)
                return null;

            var entries = await audit.ListByRuleIdAsync(ruleId, cancellationToken);
            return entries.Select(RuleAuditMappings.ToResponse).ToArray();
        }
    }
}
