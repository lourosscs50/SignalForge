using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>
/// Downstream ChronoFlow delivery failures must not prevent SignalForge from persisting lifecycle truth.
/// The HTTP publisher swallows errors; alert creation after matching ingest must still succeed.
/// </summary>
public sealed class ChronoFlowDownDoesNotBlockLifecycleTests
{
    [Fact]
    public async Task Signal_ingest_creates_alert_when_ChronoFlow_HTTP_unreachable()
    {
        using var app = new SignalForgeWebAppFactoryChronoFlowHttp();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "cf-down@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "Match temperature",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, ruleResponse.StatusCode);
        var ruleJson = await ruleResponse.Content.ReadAsStringAsync();
        using var ruleDoc = JsonDocument.Parse(ruleJson);
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        signalResponse.EnsureSuccessStatusCode();
        var signalJson = await signalResponse.Content.ReadAsStringAsync();
        using var signalDoc = JsonDocument.Parse(signalJson);
        var signalId = signalDoc.RootElement.GetProperty("id").GetGuid();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        var found = false;
        foreach (var el in alertsDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId && el.GetProperty("ruleId").GetGuid() == ruleId)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }
}
