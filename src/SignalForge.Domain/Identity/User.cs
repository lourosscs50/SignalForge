namespace SignalForge.Domain;

public sealed record User(
    Guid Id,
    string Email,
    string DisplayName,
    string PasswordHash,
    DateTime CreatedAtUtc
);

