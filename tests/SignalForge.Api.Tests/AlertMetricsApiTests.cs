using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 5.3A: GET /alerts/metrics and derived lifecycle fields on alert reads.</summary>
public sealed class AlertMetricsApiTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    private static async Task<Guid> CreateAlertWithTypeAsync(HttpClient client, string matchValue, string type)
    {
        await client.PostAsJsonAsync("/rules", new
        {
            name = $"M-{matchValue}",
            ruleType = "SignalTypeEquals",
            matchValue,
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "src",
            type,
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });
        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task A_GET_alerts_metrics_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/alerts/metrics");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task B_GET_alerts_metrics_empty_database_counts_zero_and_null_avgs()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"m-empty-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/alerts/metrics");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(0, root.GetProperty("totalAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("openAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("acknowledgedUnresolvedAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("resolvedAlerts").GetInt32());
        Assert.Equal(0, root.GetProperty("reopenedAlerts").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("averageTimeToAcknowledgeSeconds").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("averageTimeToResolveSeconds").ValueKind);
    }

    [Fact]
    public async Task C_GET_alerts_metrics_reflects_mixed_lifecycle_after_mutations()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"m-mix-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateAlertWithTypeAsync(client, "mx1", "mx1");
        var ackId = await CreateAlertWithTypeAsync(client, "mx2", "mx2");
        await client.PostAsJsonAsync($"/alerts/{ackId}/acknowledge", new { });
        var resId = await CreateAlertWithTypeAsync(client, "mx3", "mx3");
        await client.PostAsJsonAsync($"/alerts/{resId}/resolve", new { });
        var reopenId = await CreateAlertWithTypeAsync(client, "mx4", "mx4");
        await client.PostAsJsonAsync($"/alerts/{reopenId}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{reopenId}/reopen", new { });

        var metrics = await client.GetAsync("/alerts/metrics");
        metrics.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await metrics.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(4, root.GetProperty("totalAlerts").GetInt32());
        // mx1 never acked; mx4 is unresolved after reopen and was never acked — both count as open.
        Assert.Equal(2, root.GetProperty("openAlerts").GetInt32());
        Assert.Equal(1, root.GetProperty("acknowledgedUnresolvedAlerts").GetInt32());
        Assert.Equal(1, root.GetProperty("resolvedAlerts").GetInt32());
        Assert.Equal(1, root.GetProperty("reopenedAlerts").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("averageTimeToAcknowledgeSeconds").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("averageTimeToResolveSeconds").ValueKind);
    }

    [Fact]
    public async Task D_GET_alerts_and_GET_alerts_id_include_matching_derived_fields_and_HasBeenReopened()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"m-map-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var id = await CreateAlertWithTypeAsync(client, "map1", "map1");
        await client.PostAsJsonAsync($"/alerts/{id}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/reopen", new { });

        var list = await client.GetAsync("/alerts?pageSize=50");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = listDoc.RootElement.GetProperty("items").EnumerateArray().First(e => e.GetProperty("id").GetGuid() == id);

        var detail = await client.GetAsync($"/alerts/{id}");
        detail.EnsureSuccessStatusCode();
        using var detailDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var d = detailDoc.RootElement;

        Assert.True(item.GetProperty("hasBeenReopened").GetBoolean());
        Assert.True(d.GetProperty("hasBeenReopened").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("timeToResolveSeconds").ValueKind);
        Assert.Equal(JsonValueKind.Null, d.GetProperty("timeToResolveSeconds").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("timeToAcknowledgeSeconds").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, d.GetProperty("timeToAcknowledgeSeconds").ValueKind);
        Assert.Equal(
            item.GetProperty("timeToAcknowledgeSeconds").GetDouble(),
            d.GetProperty("timeToAcknowledgeSeconds").GetDouble());
        Assert.Equal("Acknowledged", item.GetProperty("currentStatus").GetString());
        Assert.Equal("Acknowledged", d.GetProperty("currentStatus").GetString());
        var ageList = item.GetProperty("ageSeconds").GetDouble();
        var ageDetail = d.GetProperty("ageSeconds").GetDouble();
        Assert.True(ageList > 0);
        Assert.True(ageDetail > 0);
        Assert.InRange(Math.Abs(ageList - ageDetail), 0, 3);
    }

    [Fact]
    public async Task E_after_resolve_and_reopen_without_ack_CurrentStatus_is_Open()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"m-open-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var id = await CreateAlertWithTypeAsync(client, "openre", "openre");
        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/reopen", new { });

        var detail = await client.GetAsync($"/alerts/{id}");
        detail.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("Open", doc.RootElement.GetProperty("currentStatus").GetString());
        Assert.False(doc.RootElement.GetProperty("isResolved").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isAcknowledged").GetBoolean());
    }
}
