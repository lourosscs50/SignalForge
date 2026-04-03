using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SignalForge.Application;
using SignalForge.Infrastructure.Automation;

namespace SignalForge.Api.Tests;

public sealed class AlertLifecycleEventPublisherDiTests
{
    [Fact]
    public void Resolves_NoOp_lifecycle_publisher_by_default()
    {
        using var app = new SignalForgeWebAppFactory();
        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAlertLifecycleEventPublisher>();

        Assert.IsType<NoOpAlertLifecycleEventPublisher>(publisher);
    }

    [Fact]
    public void Resolves_logging_lifecycle_publisher_when_configured()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Automation:Publishing:AlertLifecycleEvents", "Logging");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAlertLifecycleEventPublisher>();

        Assert.IsType<LoggingAlertLifecycleEventPublisher>(publisher);
    }
}
