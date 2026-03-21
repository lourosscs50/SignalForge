namespace SignalForge.Contracts.Signals;

public sealed record IngestSignalRequest(
    string Source,
    string Type,
    DateTimeOffset TimestampUtc
);

public sealed record SignalResponse(
    Guid Id,
    string Source,
    string Type,
    DateTimeOffset TimestampUtc
);

