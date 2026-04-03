using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 6.4C: lifecycle flows drive Infrastructure HTTP publishing without mutating lifecycle on downstream failure.</summary>
public sealed class ControlAutomationTriggerIntegrationTests
{
    [Fact]
    public async Task Alert_created_after_matching_ingest_sends_one_chrono_trigger_with_normalized_payload()
    {
        using var app = new SignalForgeWebAppFactoryChronoHttpCapture();
        var capture = app.CaptureHandler;
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "chrono-cap@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Chrono cap rule",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-cap",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        Assert.Equal(1, capture.SendCount);
        Assert.NotNull(capture.LastRequestUri);
        Assert.EndsWith("control/triggers", capture.LastRequestUri!.GetLeftPart(UriPartial.Path), StringComparison.Ordinal);
        Assert.NotNull(capture.LastRequestBody);
        using var doc = JsonDocument.Parse(capture.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("AlertCreated", root.GetProperty("triggerType").GetString());
        Assert.Equal("AlertCreated", root.GetProperty("lifecycleEventType").GetString());
        Assert.Equal("Open", root.GetProperty("currentStatus").GetString());
        Assert.False(root.GetProperty("hasBeenReopened").GetBoolean());
    }

    [Fact]
    public async Task Acknowledge_and_resolve_do_not_send_additional_chrono_triggers()
    {
        using var app = new SignalForgeWebAppFactoryChronoHttpCapture();
        var capture = app.CaptureHandler;
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "chrono-nextra@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "No extra triggers",
            ruleType = "SignalTypeEquals",
            matchValue = "pressure",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s2",
            type = "pressure",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        Assert.Equal(1, capture.SendCount);

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        Assert.Equal(1, capture.SendCount);
    }

    [Fact]
    public async Task Second_acknowledge_is_idempotent_and_does_not_send_another_chrono_trigger()
    {
        using var app = new SignalForgeWebAppFactoryChronoHttpCapture();
        var capture = app.CaptureHandler;
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "chrono-idem@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Idem ack rule",
            ruleType = "SignalTypeEquals",
            matchValue = "humidity",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s3",
            type = "humidity",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        Assert.Equal(1, capture.SendCount);

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        Assert.Equal(1, capture.SendCount);
    }

    [Fact]
    public async Task Reopen_sends_second_chrono_trigger_reflecting_reopened_lifecycle_truth()
    {
        using var app = new SignalForgeWebAppFactoryChronoHttpCapture();
        var capture = app.CaptureHandler;
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "chrono-reopen@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var userId = SignalForgeApiTestHelpers.GetJwtSubject(token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Reopen rule",
            ruleType = "SignalTypeEquals",
            matchValue = "vibration",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s4",
            type = "vibration",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        Assert.Equal(1, capture.SendCount);

        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        Assert.Equal(1, capture.SendCount);

        var reopen = await client.PostAsJsonAsync($"/alerts/{alertId}/reopen", new { });
        reopen.EnsureSuccessStatusCode();

        Assert.Equal(2, capture.SendCount);
        Assert.NotNull(capture.LastRequestBody);
        using var doc = JsonDocument.Parse(capture.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("AlertReopened", root.GetProperty("lifecycleEventType").GetString());
        Assert.True(root.GetProperty("hasBeenReopened").GetBoolean());
        Assert.Equal(userId, root.GetProperty("reopenedByUserId").GetString());
    }
}
