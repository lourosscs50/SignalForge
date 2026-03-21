namespace SignalForge.Contracts.Rules;

public sealed record CreateRuleRequest(
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive
);

public sealed record RuleResponse(
    Guid Id,
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);
