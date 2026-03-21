namespace SignalForge.Domain;

public sealed record Signal(
    Guid Id,
    string Source,
    string Type,
    string Payload,
    double? Value,
    DateTime OccurredAtUtc,
    DateTime IngestedAtUtc
);

