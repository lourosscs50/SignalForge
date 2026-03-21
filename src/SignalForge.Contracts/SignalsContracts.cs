namespace SignalForge.Contracts.Signals;

public sealed record IngestSignalRequest(
    string Source,
    string Type,
    DateTimeOffset TimestampUtc,
    double? Value = null
);

public sealed record SignalResponse(
    Guid Id,
    string Source,
    string Type,
    DateTimeOffset TimestampUtc,
    double? Value
);

