using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

public sealed class DecisionVisibilityApiTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    [Fact]
    public async Task GET_decisions_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var response = await client.GetAsync("/decisions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_decisions_id_returns_404_when_unknown()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"dv-miss-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/decisions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_creates_decision_visibility_row_readable_via_API_with_stable_shape()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"dv-flow-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "DV-Rule",
            ruleType = "SignalTypeEquals",
            matchValue = "dv-type",
            isActive = true
        });

        var ingest = await client.PostAsJsonAsync("/signals", new
        {
            source = "dv-src",
            type = "dv-type",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 42.5
        });
        ingest.EnsureSuccessStatusCode();
        using var ingestDoc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        var signalId = ingestDoc.RootElement.GetProperty("id").GetGuid();

        var list = await client.GetAsync("/decisions?pageSize=50");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = listDoc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        var row = items.EnumerateArray().First(e =>
            e.GetProperty("decisionType").GetString() == "alert.created");
        var decisionId = row.GetProperty("decisionId").GetGuid();

        Assert.Equal("alert.lifecycle", row.GetProperty("decisionCategory").GetString());
        Assert.Equal(signalId, row.GetProperty("trace").GetProperty("correlationId").GetGuid());
        Assert.NotEqual(JsonValueKind.Null, row.GetProperty("trace").GetProperty("executionId").ValueKind);
        Assert.True(row.GetProperty("explanation").GetProperty("explanationAvailable").GetBoolean());

        foreach (var p in row.EnumerateObject())
        {
            Assert.DoesNotContain("prompt", p.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("chain", p.Name, StringComparison.OrdinalIgnoreCase);
        }

        var detail = await client.GetAsync($"/decisions/{decisionId}");
        detail.EnsureSuccessStatusCode();
        using var detailDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(decisionId, detailDoc.RootElement.GetProperty("decisionId").GetGuid());

        var metrics = await client.GetAsync("/decisions/metrics");
        metrics.EnsureSuccessStatusCode();
        using var mDoc = JsonDocument.Parse(await metrics.Content.ReadAsStringAsync());
        Assert.True(mDoc.RootElement.GetProperty("totalDecisions").GetInt32() >= 1);
    }

    [Fact]
    public async Task GET_decisions_filters_by_decision_type_query()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"dv-filter-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "DV-F",
            ruleType = "SignalTypeEquals",
            matchValue = "dv-f",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "dv-f",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var ad = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = ad.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var createdOnly = await client.GetAsync("/decisions?decisionType=alert.created&pageSize=50");
        createdOnly.EnsureSuccessStatusCode();
        using var cDoc = JsonDocument.Parse(await createdOnly.Content.ReadAsStringAsync());
        foreach (var e in cDoc.RootElement.GetProperty("items").EnumerateArray())
            Assert.Equal("alert.created", e.GetProperty("decisionType").GetString());

        var ackOnly = await client.GetAsync("/decisions?decisionType=alert.acknowledged&pageSize=50");
        ackOnly.EnsureSuccessStatusCode();
        using var aDoc = JsonDocument.Parse(await ackOnly.Content.ReadAsStringAsync());
        Assert.Contains(
            aDoc.RootElement.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("decisionType").GetString() == "alert.acknowledged");
    }
}
