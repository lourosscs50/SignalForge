using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class CreateRule
{
    public sealed class Handler(IRuleRepository rules, IRuleAuditRepository audit)
    {
        public Task<RuleResponse> HandleAsync(CreateRuleRequest request, CancellationToken cancellationToken)
        {
            var name = RuleValidation.NormalizeRequiredName(request.Name, nameof(request));

            var ruleType = (request.RuleType ?? string.Empty).Trim();
            if (!RuleValidation.TryNormalizeRuleType(ruleType, out var canonicalRuleType))
                throw new ArgumentException(
                    "RuleType must be SignalTypeEquals, SignalTypeContains, or SignalValueGreaterThan.",
                    nameof(request));

            var matchValue = RuleValidation.NormalizeRequiredMatchValue(request.MatchValue, nameof(request));
            RuleValidation.ValidateMatchValueForCanonicalRuleType(canonicalRuleType, matchValue, nameof(request));

            var createdAtUtc = DateTime.UtcNow;

            var rule = new Rule(
                Id: Guid.NewGuid(),
                Name: name,
                RuleType: canonicalRuleType,
                MatchValue: matchValue,
                IsActive: request.IsActive,
                IsArchived: false,
                CreatedAtUtc: createdAtUtc);

            return MapAndPersist(rule, cancellationToken);
        }

        private async Task<RuleResponse> MapAndPersist(Rule rule, CancellationToken cancellationToken)
        {
            await rules.AddAsync(rule, cancellationToken);

            await audit.AddAsync(
                new RuleAuditEntry(
                    Id: Guid.NewGuid(),
                    RuleId: rule.Id,
                    Action: RuleAuditActions.Created,
                    OccurredAtUtc: rule.CreatedAtUtc),
                cancellationToken);

            return RuleMappings.ToResponse(rule);
        }
    }
}
