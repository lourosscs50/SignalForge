using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class UnarchiveRule
{
    public sealed class Handler(IRuleRepository rules, IRuleAuditRepository audit, IDateTimeProvider clock)
    {
        public async Task<RuleResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var existing = await rules.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return null;

            if (!existing.IsArchived)
                return RuleMappings.ToResponse(existing);

            var updated = existing.Unarchive();
            await rules.UpdateAsync(updated, cancellationToken);

            await audit.AddAsync(
                new RuleAuditEntry(
                    Id: Guid.NewGuid(),
                    RuleId: updated.Id,
                    Action: RuleAuditActions.Unarchived,
                    OccurredAtUtc: clock.UtcNow),
                cancellationToken);

            return RuleMappings.ToResponse(updated);
        }
    }
}
