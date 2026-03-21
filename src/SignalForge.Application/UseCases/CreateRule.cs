using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class CreateRule
{
    public sealed class Handler(IRuleRepository rules)
    {
        public Task<RuleResponse> HandleAsync(CreateRuleRequest request, CancellationToken cancellationToken)
        {
            var conditionType = ParseConditionType(request.ConditionType);
            var createdAtUtc = DateTime.UtcNow;

            var rule = new Rule(
                Id: Guid.NewGuid(),
                Name: request.Name,
                SignalType: request.SignalType,
                ConditionType: conditionType,
                Expression: request.Expression,
                IsActive: request.IsActive,
                CreatedAtUtc: createdAtUtc
            );

            return MapAndPersist(rule, cancellationToken);
        }

        private async Task<RuleResponse> MapAndPersist(Rule rule, CancellationToken cancellationToken)
        {
            await rules.AddAsync(rule, cancellationToken);

            return new RuleResponse(
                Id: rule.Id,
                Name: rule.Name,
                SignalType: rule.SignalType,
                ConditionType: rule.ConditionType.ToString(),
                Expression: rule.Expression,
                IsActive: rule.IsActive,
                CreatedAtUtc: new DateTimeOffset(rule.CreatedAtUtc, TimeSpan.Zero)
            );
        }

        private static RuleConditionType ParseConditionType(string conditionType)
        {
            if (Enum.TryParse<RuleConditionType>(conditionType, ignoreCase: true, out var parsed))
                return parsed;

            // Keep mapping simple for now: invalid condition types fail fast.
            throw new ArgumentException("ConditionType is invalid.", nameof(conditionType));
        }
    }
}
