namespace SignalForge.Contracts.Identity;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password
);

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record AuthResponse(
    string Token
);

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string DisplayName
);

