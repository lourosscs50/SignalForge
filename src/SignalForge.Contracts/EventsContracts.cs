namespace SignalForge.Contracts.Events;

public sealed record EvaluateEventRequest(
    Guid SignalId,
    Guid RuleId
);

public sealed record EvaluateEventResponse(
    Guid EvaluationId,
    string Outcome
);

