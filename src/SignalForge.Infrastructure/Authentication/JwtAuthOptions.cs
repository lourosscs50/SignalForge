namespace SignalForge.Infrastructure.Authentication;

public static class JwtAuthOptions
{
    public const string Issuer = "signalforge";
    public const string Audience = "signalforge-api";

    // NOTE: in a real system this must come from configuration/secret storage.
    public const string SigningKey = "please-change-me-to-a-very-long-random-secret";

    public const int TokenLifetimeMinutes = 60;
}

