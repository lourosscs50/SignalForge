using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SignalForge.Contracts.Automation;
using SignalForge.Infrastructure.Automation;
using SignalForge.Infrastructure.Integration;

namespace SignalForge.Infrastructure.Tests;

public sealed class ChronoFlowHttpControlAutomationTriggerPublisherTests
{
    private static readonly ControlAutomationTriggerRequest SampleTrigger = new(
        TriggerType: "AlertCreated",
        AlertId: Guid.Parse("a1000000-0000-0000-0000-000000000001"),
        RuleId: Guid.Parse("b2000000-0000-0000-0000-000000000002"),
        SignalId: Guid.Parse("c3000000-0000-0000-0000-000000000003"),
        OccurredAtUtc: new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
        CurrentStatus: "Open",
        LifecycleEventType: "AlertCreated",
        AcknowledgedByUserId: null,
        ResolvedByUserId: null,
        ReopenedByUserId: null,
        RuleName: "Rule A",
        HasBeenReopened: false);

    [Fact]
    public async Task PublishAsync_sends_web_json_payload_matching_contract()
    {
        using var capture = new CaptureHandler();
        capture.Response = new HttpResponseMessage(HttpStatusCode.Accepted);

        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://chronoflow.test/",
            EndpointPath = "control/triggers",
            TimeoutSeconds = 30
        });

        var client = new HttpClient(capture) { BaseAddress = new Uri("https://ignored/") };
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        await publisher.PublishAsync(SampleTrigger, CancellationToken.None);

        Assert.NotNull(capture.LastRequest);
        Assert.Equal(HttpMethod.Post, capture.LastRequest!.Method);
        Assert.Equal("https://chronoflow.test/control/triggers", capture.LastRequest.RequestUri!.ToString());
        Assert.NotNull(capture.LastRequestBody);
        using var doc = JsonDocument.Parse(capture.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("AlertCreated", root.GetProperty("triggerType").GetString());
        Assert.Equal(SampleTrigger.AlertId, root.GetProperty("alertId").GetGuid());
        Assert.Equal(SampleTrigger.RuleId, root.GetProperty("ruleId").GetGuid());
        Assert.Equal(SampleTrigger.SignalId, root.GetProperty("signalId").GetGuid());
        Assert.Equal("Open", root.GetProperty("currentStatus").GetString());
        Assert.Equal("AlertCreated", root.GetProperty("lifecycleEventType").GetString());
        Assert.Equal("Rule A", root.GetProperty("ruleName").GetString());
        Assert.False(root.GetProperty("hasBeenReopened").GetBoolean());
    }

    [Fact]
    public async Task PublishAsync_sends_X_Api_Key_when_configured()
    {
        using var capture = new CaptureHandler();
        capture.Response = new HttpResponseMessage(HttpStatusCode.Accepted);

        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://chronoflow.test",
            EndpointPath = "/control/triggers",
            ApiKey = "secret-key-1"
        });

        var client = new HttpClient(capture);
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        await publisher.PublishAsync(SampleTrigger, CancellationToken.None);

        Assert.True(capture.LastRequest!.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Equal("secret-key-1", Assert.Single(values));
    }

    [Fact]
    public async Task PublishAsync_sends_Bearer_token_when_ApiKey_absent()
    {
        using var capture = new CaptureHandler();
        capture.Response = new HttpResponseMessage(HttpStatusCode.Accepted);

        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://chronoflow.test",
            BearerToken = "token-99"
        });

        var client = new HttpClient(capture);
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        await publisher.PublishAsync(SampleTrigger, CancellationToken.None);

        var auth = capture.LastRequest!.Headers.Authorization;
        Assert.NotNull(auth);
        Assert.Equal("Bearer", auth!.Scheme);
        Assert.Equal("token-99", auth.Parameter);
    }

    [Fact]
    public async Task PublishAsync_when_disabled_does_not_send_http()
    {
        using var capture = new CaptureHandler();
        capture.Response = new HttpResponseMessage(HttpStatusCode.Accepted);

        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = false,
            BaseUrl = "https://chronoflow.test",
        });

        var client = new HttpClient(capture);
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        await publisher.PublishAsync(SampleTrigger, CancellationToken.None);

        Assert.Null(capture.LastRequest);
    }

    [Fact]
    public async Task PublishAsync_when_http_fails_does_not_throw()
    {
        using var capture = new CaptureHandler();
        capture.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        };

        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://chronoflow.test",
        });

        var client = new HttpClient(capture);
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        var ex = await Record.ExceptionAsync(() => publisher.PublishAsync(SampleTrigger, CancellationToken.None));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishAsync_when_http_throws_does_not_throw_to_caller()
    {
        using var capture = new CaptureHandler { ThrowOnSend = true };
        var options = Options.Create(new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://chronoflow.test",
        });

        var client = new HttpClient(capture);
        var publisher = new ChronoFlowHttpControlAutomationTriggerPublisher(
            client,
            options,
            NullLogger<ChronoFlowHttpControlAutomationTriggerPublisher>.Instance);

        var ex = await Record.ExceptionAsync(() => publisher.PublishAsync(SampleTrigger, CancellationToken.None));
        Assert.Null(ex);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public HttpResponseMessage? Response { get; set; }

        public bool ThrowOnSend { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (ThrowOnSend)
                throw new HttpRequestException("simulated network failure");
            return await Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
