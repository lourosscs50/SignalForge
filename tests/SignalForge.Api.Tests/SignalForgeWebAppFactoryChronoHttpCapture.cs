using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace SignalForge.Api.Tests;

/// <summary>
/// ChronoFlow HTTP delivery enabled with a synthetic primary handler so tests assert outbound requests without the network.
/// </summary>
public sealed class SignalForgeWebAppFactoryChronoHttpCapture : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = $"SignalForgeTests_{Guid.NewGuid():N}";

    public ChronoFlowHttpCapturePrimaryHandler CaptureHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:InMemoryDatabaseName", _inMemoryDatabaseName);
        builder.UseSetting("ChronoFlow:ControlTriggers:Enabled", "true");
        builder.UseSetting("ChronoFlow:ControlTriggers:BaseUrl", "https://chronoflow.capture.test");
        builder.UseSetting("ChronoFlow:ControlTriggers:EndpointPath", "control/triggers");
        builder.UseSetting("ChronoFlow:ControlTriggers:TimeoutSeconds", "30");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                new ReplacePrimaryHttpHandlerFilter(CaptureHandler));
        });
    }

    private sealed class ReplacePrimaryHttpHandlerFilter(HttpMessageHandler handler) : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
            builder =>
            {
                next(builder);
                builder.PrimaryHandler = handler;
            };
    }
}

/// <summary>Records ChronoFlow POST attempts from the typed HttpClient pipeline.</summary>
public sealed class ChronoFlowHttpCapturePrimaryHandler : HttpMessageHandler
{
    public int SendCount { get; private set; }

    public Uri? LastRequestUri { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SendCount++;
        LastRequestUri = request.RequestUri;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.Accepted);
    }
}
