namespace SignalForge.Domain;

public sealed record Signal(
    Guid Id,
    string Source,
    string Type,
    string Payload,
    DateTime OccurredAtUtc,
    DateTime IngestedAtUtc
);

