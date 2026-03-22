using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class DeactivateRule
{
    public sealed class Handler(IRuleRepository rules)
    {
        public async Task<RuleResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var existing = await rules.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return null;

            var updated = existing.Deactivate();
            await rules.UpdateAsync(updated, cancellationToken);
            return RuleMappings.ToResponse(updated);
        }
    }
}
