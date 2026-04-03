using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SignalForge.Application;
using SignalForge.Infrastructure.Automation;

namespace SignalForge.Api.Tests;

public sealed class ControlAutomationTriggerPublisherDiTests
{
    [Fact]
    public void Resolves_NoOp_publisher_when_ChronoFlow_integration_disabled()
    {
        using var app = new SignalForgeWebAppFactory();
        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<NoOpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_http_publisher_when_ChronoFlow_enabled_and_base_url_configured()
    {
        using var app = new SignalForgeWebAppFactoryChronoFlowHttp();
        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<ChronoFlowHttpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_NoOp_when_enabled_but_base_url_empty()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
            b.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<NoOpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_logging_trigger_publisher_when_configured()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
            b.UseSetting("Automation:Publishing:ControlAutomationTriggers", "Logging"));

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<LoggingControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_NoOp_when_explicit_NoOp_even_if_ChronoFlow_base_url_configured()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Automation:Publishing:ControlAutomationTriggers", "NoOp");
            b.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
            b.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "https://chronoflow.example/");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<NoOpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_NoOp_when_explicit_ChronoFlowHttp_but_BaseUrl_incomplete()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Automation:Publishing:ControlAutomationTriggers", "ChronoFlowHttp");
            b.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
            b.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<NoOpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_http_publisher_when_explicit_ChronoFlowHttp_and_delivery_config_complete()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Automation:Publishing:ControlAutomationTriggers", "ChronoFlowHttp");
            b.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
            b.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "https://chronoflow.example/");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<ChronoFlowHttpControlAutomationTriggerPublisher>(publisher);
    }

    [Fact]
    public void Resolves_NoOp_when_explicit_ChronoFlowHttp_but_integration_disabled()
    {
        using var app = new SignalForgeWebAppFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Automation:Publishing:ControlAutomationTriggers", "ChronoFlowHttp");
            b.UseSetting("ChronoFlow:ControlTriggers:Enabled", "false");
            b.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "https://chronoflow.example/");
        });

        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IControlAutomationTriggerPublisher>();

        Assert.IsType<NoOpControlAutomationTriggerPublisher>(publisher);
    }
}
