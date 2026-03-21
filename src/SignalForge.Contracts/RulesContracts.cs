namespace SignalForge.Contracts.Rules;

public sealed record CreateRuleRequest(
    string Name,
    string SignalType,
    string ConditionType,
    string Expression,
    bool IsActive
);

public sealed record RuleResponse(
    Guid Id,
    string Name,
    string SignalType,
    string ConditionType,
    string Expression,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);

