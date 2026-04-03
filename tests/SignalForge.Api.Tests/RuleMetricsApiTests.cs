using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 5.3B: GET /rules/{id}/metrics.</summary>
public sealed class RuleMetricsApiTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    [Fact]
    public async Task A_GET_rules_id_metrics_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/metrics");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task B_GET_rules_id_metrics_returns_404_when_rule_missing()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"rm-404-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/metrics");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task C_GET_rules_id_metrics_existing_rule_no_alerts_zeros_and_null_avgs()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"rm-empty-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "RmEmpty",
            ruleType = "SignalTypeEquals",
            matchValue = "rm-e-type",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var cdoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var ruleId = cdoc.RootElement.GetProperty("id").GetGuid();
        var ruleName = cdoc.RootElement.GetProperty("name").GetString();

        var response = await client.GetAsync($"/rules/{ruleId}/metrics");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(ruleId, root.GetProperty("ruleId").GetGuid());
        Assert.Equal(ruleName, root.GetProperty("ruleName").GetString());
        Assert.Equal(0, root.GetProperty("totalAlertsGenerated").GetInt32());
        Assert.Equal(0, root.GetProperty("openAlerts").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("averageTimeToAcknowledgeSeconds").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("averageTimeToResolveSeconds").ValueKind);
    }

    [Fact]
    public async Task D_GET_rules_id_metrics_reflects_alerts_after_mutations_and_names_follow_rule()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"rm-mix-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "RmMixOriginal",
            ruleType = "SignalTypeEquals",
            matchValue = "rm-mix-t",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var cr = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var ruleId = cr.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "rm-mix-t",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "rm-mix-t",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 2.0
        });

        var list = await client.GetAsync($"/alerts?ruleId={ruleId}&pageSize=10");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = listDoc.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        var ackId = items[0].GetProperty("id").GetGuid();
        var openId = items[1].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{ackId}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{openId}/resolve", new { });

        await client.PutAsJsonAsync($"/rules/{ruleId}", new
        {
            name = "RmMixRenamed",
            matchValue = "rm-mix-t"
        });

        var metrics = await client.GetAsync($"/rules/{ruleId}/metrics");
        metrics.EnsureSuccessStatusCode();
        using var mdoc = JsonDocument.Parse(await metrics.Content.ReadAsStringAsync());
        var root = mdoc.RootElement;
        Assert.Equal("RmMixRenamed", root.GetProperty("ruleName").GetString());
        Assert.Equal(2, root.GetProperty("totalAlertsGenerated").GetInt32());
        Assert.Equal(0, root.GetProperty("openAlerts").GetInt32());
        Assert.Equal(1, root.GetProperty("acknowledgedUnresolvedAlerts").GetInt32());
        Assert.Equal(1, root.GetProperty("resolvedAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("reopenedAlerts").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("averageTimeToAcknowledgeSeconds").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("averageTimeToResolveSeconds").ValueKind);
    }

    [Fact]
    public async Task E_reopened_alert_not_resolved_in_rule_metrics()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"rm-reo-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "RmReo",
            ruleType = "SignalTypeEquals",
            matchValue = "rm-reo-t",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var cr = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var ruleId = cr.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "rm-reo-t",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var ld = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = ld.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{alertId}/reopen", new { });

        var metrics = await client.GetAsync($"/rules/{ruleId}/metrics");
        metrics.EnsureSuccessStatusCode();
        using var md = JsonDocument.Parse(await metrics.Content.ReadAsStringAsync());
        var root = md.RootElement;
        Assert.Equal(1, root.GetProperty("reopenedAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("resolvedAlerts").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("averageTimeToResolveSeconds").ValueKind);
    }
}
