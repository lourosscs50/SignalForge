using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Infrastructure.Integration;

namespace SignalForge.Infrastructure.Automation;

public static class AutomationPublisherRegistrations
{
    /// <summary>Registers automation publishers, HTTP client, and coordinator wiring per configuration.</summary>
    public static IServiceCollection AddSignalForgeAutomationPublishers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AutomationPublishingOptions>(
            configuration.GetSection(AutomationPublishingOptions.SectionName));
        services.Configure<ChronoFlowControlTriggerIntegrationOptions>(
            configuration.GetSection(ChronoFlowControlTriggerIntegrationOptions.SectionName));

        services.AddScoped<IAlertAutomationPolicy, DefaultAlertAutomationPolicy>();

        services.AddScoped<NoOpAlertLifecycleEventPublisher>();
        services.AddScoped<LoggingAlertLifecycleEventPublisher>();
        services.AddScoped<IAlertLifecycleEventPublisher>(sp => ResolveLifecyclePublisher(sp));

        services.AddScoped<NoOpControlAutomationTriggerPublisher>();
        services.AddScoped<LoggingControlAutomationTriggerPublisher>();
        services.AddHttpClient<ChronoFlowHttpControlAutomationTriggerPublisher>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ChronoFlowControlTriggerIntegrationOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
        });
        services.AddScoped<IControlAutomationTriggerPublisher>(sp => ResolveControlTriggerPublisher(sp));

        services.AddScoped<AlertLifecycleAutomationCoordinator>();

        return services;
    }

    private static IAlertLifecycleEventPublisher ResolveLifecyclePublisher(IServiceProvider sp)
    {
        var mode = sp.GetRequiredService<IOptions<AutomationPublishingOptions>>().Value.AlertLifecycleEvents;
        return mode switch
        {
            AlertLifecycleEventPublisherMode.Logging => sp.GetRequiredService<LoggingAlertLifecycleEventPublisher>(),
            _ => sp.GetRequiredService<NoOpAlertLifecycleEventPublisher>()
        };
    }

    private static IControlAutomationTriggerPublisher ResolveControlTriggerPublisher(IServiceProvider sp)
    {
        var publishing = sp.GetRequiredService<IOptions<AutomationPublishingOptions>>().Value;
        var chron = sp.GetRequiredService<IOptions<ChronoFlowControlTriggerIntegrationOptions>>().Value;
        var mode = EffectiveControlTriggerMode(publishing, chron);
        return mode switch
        {
            ControlAutomationTriggerPublisherMode.Logging => sp.GetRequiredService<LoggingControlAutomationTriggerPublisher>(),
            ControlAutomationTriggerPublisherMode.ChronoFlowHttp when IsChronoFlowHttpDeliveryReady(chron) =>
                sp.GetRequiredService<ChronoFlowHttpControlAutomationTriggerPublisher>(),
            ControlAutomationTriggerPublisherMode.ChronoFlowHttp => sp.GetRequiredService<NoOpControlAutomationTriggerPublisher>(),
            _ => sp.GetRequiredService<NoOpControlAutomationTriggerPublisher>()
        };
    }

    /// <summary>Matches <see cref="ChronoFlowHttpControlAutomationTriggerPublisher"/> preconditions so DI never wires a dead HTTP client.</summary>
    internal static bool IsChronoFlowHttpDeliveryReady(ChronoFlowControlTriggerIntegrationOptions chron) =>
        chron.Enabled && !string.IsNullOrWhiteSpace(chron.BaseUrl);

    /// <summary>Explicit <see cref="AutomationPublishingOptions.ControlAutomationTriggers"/> wins; otherwise legacy ChronoFlow flags apply.</summary>
    internal static ControlAutomationTriggerPublisherMode EffectiveControlTriggerMode(
        AutomationPublishingOptions publishing,
        ChronoFlowControlTriggerIntegrationOptions chron)
    {
        if (publishing.ControlAutomationTriggers.HasValue)
            return publishing.ControlAutomationTriggers.Value;
        if (chron.Enabled && !string.IsNullOrWhiteSpace(chron.BaseUrl))
            return ControlAutomationTriggerPublisherMode.ChronoFlowHttp;
        return ControlAutomationTriggerPublisherMode.NoOp;
    }
}
