using SignalForge.Application;
using SignalForge.Contracts.Rules;

namespace SignalForge.Application.UseCases;

public static class GetRule
{
    public sealed class Handler(IRuleRepository rules)
    {
        public async Task<RuleResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var rule = await rules.GetByIdAsync(id, cancellationToken);
            return rule is null ? null : RuleMappings.ToResponse(rule);
        }
    }
}
