using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class DeactivateRule
{
    public sealed class Handler(IRuleRepository rules, IRuleAuditRepository audit, IDateTimeProvider clock)
    {
        public async Task<RuleResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var existing = await rules.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return null;

            var updated = existing.Deactivate();
            await rules.UpdateAsync(updated, cancellationToken);

            if (existing.IsActive)
            {
                await audit.AddAsync(
                    new RuleAuditEntry(
                        Id: Guid.NewGuid(),
                        RuleId: updated.Id,
                        Action: RuleAuditActions.Deactivated,
                        OccurredAtUtc: clock.UtcNow),
                    cancellationToken);
            }

            return RuleMappings.ToResponse(updated);
        }
    }
}
