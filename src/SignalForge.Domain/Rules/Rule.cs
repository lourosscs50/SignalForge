namespace SignalForge.Domain;

public sealed record Rule(
    Guid Id,
    string Name,
    string SignalType,
    RuleConditionType ConditionType,
    string Expression,
    bool IsActive,
    DateTime CreatedAtUtc
);

public enum RuleConditionType
{
    Threshold,
    Equals,
    Contains,
    Regex
}

