using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SignalForge.Api.Tests;

public sealed class SignalForgeWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = $"SignalForgeTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:InMemoryDatabaseName", _inMemoryDatabaseName);
    }
}
