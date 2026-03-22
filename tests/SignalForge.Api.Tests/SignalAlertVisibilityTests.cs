using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

public sealed class SignalAlertVisibilityTests
{
    [Fact]
    public async Task Get_signals_id_returns_signal_when_exists()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "sigvis@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ingest = await client.PostAsJsonAsync("/signals", new
        {
            source = "src-a",
            type = "type-a",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.5
        });
        ingest.EnsureSuccessStatusCode();
        using var ingDoc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        var id = ingDoc.RootElement.GetProperty("id").GetGuid();

        var get = await client.GetAsync($"/signals/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("src-a", doc.RootElement.GetProperty("source").GetString());
        Assert.Equal("type-a", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(1.5, doc.RootElement.GetProperty("value").GetDouble());
    }

    [Fact]
    public async Task Get_signals_id_returns_404_when_missing()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "sigvis2@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/signals/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_alerts_id_returns_detail_with_rule_and_signal_summaries()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "alertvis@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "Lineage Rule",
            ruleType = "SignalTypeEquals",
            matchValue = "heat",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        var sigRes = await client.PostAsJsonAsync("/signals", new
        {
            source = "boiler-1",
            type = "heat",
            timestampUtc = DateTimeOffset.UtcNow
        });
        sigRes.EnsureSuccessStatusCode();
        using var sigDoc = JsonDocument.Parse(await sigRes.Content.ReadAsStringAsync());
        var signalId = sigDoc.RootElement.GetProperty("id").GetGuid();

        var listAlerts = await client.GetAsync("/alerts?pageSize=10");
        listAlerts.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await listAlerts.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var get = await client.GetAsync($"/alerts/{alertId}");
        get.EnsureSuccessStatusCode();
        using var detail = JsonDocument.Parse(await get.Content.ReadAsStringAsync());

        Assert.Equal(alertId, detail.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(ruleId, detail.RootElement.GetProperty("ruleId").GetGuid());
        Assert.Equal(signalId, detail.RootElement.GetProperty("signalId").GetGuid());

        var rule = detail.RootElement.GetProperty("rule");
        Assert.Equal(ruleId, rule.GetProperty("id").GetGuid());
        Assert.Equal("Lineage Rule", rule.GetProperty("name").GetString());
        Assert.Equal("SignalTypeEquals", rule.GetProperty("ruleType").GetString());
        Assert.True(rule.GetProperty("isActive").GetBoolean());

        var signal = detail.RootElement.GetProperty("signal");
        Assert.Equal(signalId, signal.GetProperty("id").GetGuid());
        Assert.Equal("boiler-1", signal.GetProperty("source").GetString());
        Assert.Equal("heat", signal.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Get_alerts_id_returns_404_when_missing()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/alerts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_alerts_list_items_remain_compact_without_embedded_rule_or_signal_objects()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "compact@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var first = doc.RootElement.GetProperty("items")[0];
        Assert.False(first.TryGetProperty("rule", out _));
        Assert.False(first.TryGetProperty("signal", out _));
        Assert.True(first.TryGetProperty("ruleId", out _));
        Assert.True(first.TryGetProperty("signalId", out _));
    }
}
