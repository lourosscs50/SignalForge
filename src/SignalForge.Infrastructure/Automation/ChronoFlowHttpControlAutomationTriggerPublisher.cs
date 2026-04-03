using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalForge.Application;
using SignalForge.Contracts.Automation;
using SignalForge.Infrastructure.Integration;

namespace SignalForge.Infrastructure.Automation;

/// <summary>Sends <see cref="ControlAutomationTriggerRequest"/> to ChronoFlow over HTTP. Failures are logged and never propagated to the application layer.</summary>
public sealed class ChronoFlowHttpControlAutomationTriggerPublisher : IControlAutomationTriggerPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<ChronoFlowControlTriggerIntegrationOptions> _options;
    private readonly ILogger<ChronoFlowHttpControlAutomationTriggerPublisher> _logger;

    public ChronoFlowHttpControlAutomationTriggerPublisher(
        HttpClient httpClient,
        IOptions<ChronoFlowControlTriggerIntegrationOptions> options,
        ILogger<ChronoFlowHttpControlAutomationTriggerPublisher> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(ControlAutomationTriggerRequest request, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.BaseUrl))
            return;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
            timeoutCts.CancelAfter(timeout);

            var path = (opts.EndpointPath ?? string.Empty).Trim();
            path = path.TrimStart('/');
            var baseUri = new Uri(opts.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            var uri = string.IsNullOrEmpty(path) ? baseUri : new Uri(baseUri, path);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
            ApplyAuth(httpRequest, opts);
            httpRequest.Content = JsonContent.Create(request, options: SerializerOptions);

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                _logger.LogWarning(
                    "ChronoFlow control trigger delivery failed with HTTP {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    body);
            }
            else
            {
                _logger.LogInformation(
                    "ChronoFlow control trigger delivered successfully for alert {AlertId} ({LifecycleEventType}).",
                    request.AlertId,
                    request.LifecycleEventType);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ChronoFlow control trigger delivery timed out for alert {AlertId}.", request.AlertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChronoFlow control trigger delivery failed for alert {AlertId}.", request.AlertId);
        }
    }

    private static void ApplyAuth(HttpRequestMessage httpRequest, ChronoFlowControlTriggerIntegrationOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", opts.ApiKey);
        else if (!string.IsNullOrWhiteSpace(opts.BearerToken))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.BearerToken);
    }
}
