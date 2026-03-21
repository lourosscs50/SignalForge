using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalForge.Application;
using SignalForge.Infrastructure.Authentication;

namespace SignalForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSignalForgeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}