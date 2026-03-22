using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalForge.Application;

namespace SignalForge.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSignalForgePersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<SignalForgeDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing"))
            {
                // WebApplicationFactory sets ConnectionStrings:InMemoryDatabaseName per host instance so
                // parallel tests do not share one global in-memory store; all scopes in that host share the name.
                var inMemoryName = configuration["ConnectionStrings:InMemoryDatabaseName"];
                if (string.IsNullOrWhiteSpace(inMemoryName))
                    throw new InvalidOperationException("ConnectionStrings:InMemoryDatabaseName is required in Testing.");
                options.UseInMemoryDatabase(inMemoryName);
            }
            else
            {
                var connectionString = configuration.GetConnectionString("PostgreSQL");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:PostgreSQL is required when not using the Testing environment.");
                }

                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRuleRepository, EfRuleRepository>();
        services.AddScoped<IRuleAuditRepository, EfRuleAuditRepository>();
        services.AddScoped<ISignalRepository, EfSignalRepository>();
        services.AddScoped<IAlertRepository, EfAlertRepository>();

        return services;
    }
}
