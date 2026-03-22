using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class UpdateRule
{
    public sealed class Handler(IRuleRepository rules, IRuleAuditRepository audit, IDateTimeProvider clock)
    {
        public async Task<RuleResponse?> HandleAsync(
            Guid id,
            UpdateRuleRequest request,
            CancellationToken cancellationToken)
        {
            var existing = await rules.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return null;

            var name = RuleValidation.NormalizeRequiredName(request.Name, nameof(request));
            var matchValue = RuleValidation.NormalizeRequiredMatchValue(request.MatchValue, nameof(request));
            RuleValidation.ValidateMatchValueForCanonicalRuleType(existing.RuleType, matchValue, nameof(request));

            if (string.Equals(existing.Name, name, StringComparison.Ordinal)
                && string.Equals(existing.MatchValue, matchValue, StringComparison.Ordinal))
            {
                return RuleMappings.ToResponse(existing);
            }

            var updated = existing.UpdateDetails(name, matchValue);
            await rules.UpdateAsync(updated, cancellationToken);

            await audit.AddAsync(
                new RuleAuditEntry(
                    Id: Guid.NewGuid(),
                    RuleId: updated.Id,
                    Action: RuleAuditActions.Updated,
                    OccurredAtUtc: clock.UtcNow,
                    UpdateDetail: new RuleAuditUpdateDetail(
                        PreviousName: existing.Name,
                        NewName: name,
                        PreviousMatchValue: existing.MatchValue,
                        NewMatchValue: matchValue)),
                cancellationToken);

            return RuleMappings.ToResponse(updated);
        }
    }
}
