using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SignalForge.Api.Tests;

/// <summary>Testing host with ChronoFlow HTTP delivery enabled (used for DI and resilience checks).</summary>
public sealed class SignalForgeWebAppFactoryChronoFlowHttp : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = $"SignalForgeTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:InMemoryDatabaseName", _inMemoryDatabaseName);
        builder.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
        builder.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "http://127.0.0.1:1");
        builder.UseSetting("ChronoFlow:ControlTriggers:EndpointPath", "control/triggers");
        builder.UseSetting("ChronoFlow:ControlTriggers:TimeoutSeconds", "1");
    }
}
