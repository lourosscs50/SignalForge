using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class CreateRule
{
    public sealed class Handler(IRuleRepository rules)
    {
        public Task<RuleResponse> HandleAsync(CreateRuleRequest request, CancellationToken cancellationToken)
        {
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is required.", nameof(request));

            var ruleType = (request.RuleType ?? string.Empty).Trim();
            if (!TryNormalizeRuleType(ruleType, out var canonicalRuleType))
                throw new ArgumentException(
                    "RuleType must be SignalTypeEquals, SignalTypeContains, or SignalValueGreaterThan.",
                    nameof(request));

            var matchValue = (request.MatchValue ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(matchValue))
                throw new ArgumentException("MatchValue is required.", nameof(request));

            var createdAtUtc = DateTime.UtcNow;

            var rule = new Rule(
                Id: Guid.NewGuid(),
                Name: name,
                RuleType: canonicalRuleType,
                MatchValue: matchValue,
                IsActive: request.IsActive,
                CreatedAtUtc: createdAtUtc);

            return MapAndPersist(rule, cancellationToken);
        }

        private static bool TryNormalizeRuleType(string ruleType, out string canonicalRuleType)
        {
            if (string.Equals(ruleType, RuleTypes.SignalTypeEquals, StringComparison.OrdinalIgnoreCase))
            {
                canonicalRuleType = RuleTypes.SignalTypeEquals;
                return true;
            }

            if (string.Equals(ruleType, RuleTypes.SignalTypeContains, StringComparison.OrdinalIgnoreCase))
            {
                canonicalRuleType = RuleTypes.SignalTypeContains;
                return true;
            }

            if (string.Equals(ruleType, RuleTypes.SignalValueGreaterThan, StringComparison.OrdinalIgnoreCase))
            {
                canonicalRuleType = RuleTypes.SignalValueGreaterThan;
                return true;
            }

            canonicalRuleType = string.Empty;
            return false;
        }

        private async Task<RuleResponse> MapAndPersist(Rule rule, CancellationToken cancellationToken)
        {
            await rules.AddAsync(rule, cancellationToken);

            return new RuleResponse(
                Id: rule.Id,
                Name: rule.Name,
                RuleType: rule.RuleType,
                MatchValue: rule.MatchValue,
                IsActive: rule.IsActive,
                CreatedAtUtc: new DateTimeOffset(rule.CreatedAtUtc, TimeSpan.Zero));
        }
    }
}
