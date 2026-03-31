using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalForge.Application;

namespace SignalForge.Api.Tests;

/// <summary>Test host where JWT validates but <see cref="ICurrentUser"/> supplies no user id.</summary>
public sealed class SignalForgeWebAppFactoryMissingActor : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = $"SignalForgeTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:InMemoryDatabaseName", _inMemoryDatabaseName);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ => new MissingActorCurrentUser());
        });
    }

    private sealed class MissingActorCurrentUser : ICurrentUser
    {
        public string? UserId => null;
    }
}
