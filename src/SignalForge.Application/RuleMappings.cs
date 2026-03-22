using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class RuleMappings
{
    public static RuleResponse ToResponse(Rule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.RuleType,
            rule.MatchValue,
            rule.IsActive,
            new DateTimeOffset(rule.CreatedAtUtc, TimeSpan.Zero));
}
