namespace SignalForge.Infrastructure.Integration;

/// <summary>HTTP delivery settings for normalized control automation triggers to ChronoFlow (infrastructure-only).</summary>
public sealed class ChronoFlowControlTriggerIntegrationOptions
{
    public const string SectionName = "ChronoFlow:ControlTriggers";

    /// <summary>When false or when <see cref="BaseUrl"/> is empty, SignalForge must not activate the HTTP publisher for this integration.</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL for ChronoFlow (no trailing path segment).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Path segment appended to <see cref="BaseUrl"/> (with or without leading slash).</summary>
    public string EndpointPath { get; set; } = "control/triggers";

    /// <summary>Optional service API key; sent as X-Api-Key when set. Takes precedence over <see cref="BearerToken"/>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Optional bearer token; used only when <see cref="ApiKey"/> is not set.</summary>
    public string? BearerToken { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}
