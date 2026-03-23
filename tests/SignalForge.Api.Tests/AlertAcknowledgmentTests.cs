using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.3: POST /alerts/{id}/acknowledge; acknowledgment state on list/detail.</summary>
public sealed class AlertAcknowledgmentTests
{
    private static async Task<Guid> CreateAlertAsync(HttpClient client)
    {
        await client.PostAsJsonAsync("/rules", new
        {
            name = "Ack-R",
            ruleType = "SignalTypeEquals",
            matchValue = "ack-type",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "ack-type",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task A_POST_alerts_id_acknowledge_returns_200_with_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-a@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var response = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task B_C_POST_alerts_id_acknowledge_sets_IsAcknowledged_and_AcknowledgedAtUtc()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-bc@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var ack = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        ack.EnsureSuccessStatusCode();
        using var ackDoc = JsonDocument.Parse(await ack.Content.ReadAsStringAsync());
        Assert.True(ackDoc.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.NotEqual(default, ackDoc.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task D_Re_acknowledge_is_idempotent_and_preserves_AcknowledgedAtUtc()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-d@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var first = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        first.EnsureSuccessStatusCode();
        using var firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var ts = firstDoc.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset();

        await Task.Delay(50);

        var second = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        second.EnsureSuccessStatusCode();
        using var secondDoc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(ts, secondDoc.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task E_POST_alerts_id_acknowledge_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/alerts/{Guid.NewGuid()}/acknowledge", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task F_POST_alerts_id_acknowledge_returns_404_when_alert_missing()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/alerts/{Guid.NewGuid()}/acknowledge", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task G_GET_alerts_includes_acknowledgment_fields()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-g@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = listDoc.RootElement.GetProperty("items").EnumerateArray()
            .First(e => e.GetProperty("id").GetGuid() == alertId);
        Assert.False(item.GetProperty("isAcknowledged").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("acknowledgedAtUtc").ValueKind);
    }

    [Fact]
    public async Task H_GET_alerts_id_includes_acknowledgment_fields()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.False(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.Equal(JsonValueKind.Null, d.RootElement.GetProperty("acknowledgedAtUtc").ValueKind);
    }

    [Fact]
    public async Task I_Enriched_rule_and_signal_summaries_remain_after_acknowledgment()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-I",
            ruleType = "SignalTypeEquals",
            matchValue = "t-i",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "src-i",
            type = "t-i",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 2.0
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
        var rule = d.RootElement.GetProperty("rule");
        Assert.Equal(ruleId, rule.GetProperty("id").GetGuid());
        Assert.Equal("R-I", rule.GetProperty("name").GetString());
        Assert.NotEqual(default, d.RootElement.GetProperty("signal").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task M_New_alert_is_unacknowledged_by_default()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-m@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);

        var get = await client.GetAsync($"/alerts/{alertId}");
        get.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.False(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.Equal(JsonValueKind.Null, d.RootElement.GetProperty("acknowledgedAtUtc").ValueKind);
    }

    [Fact]
    public async Task N_Acknowledged_alert_remains_visible_in_list_and_detail()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-n@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client);
        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var list = await client.GetAsync("/alerts?pageSize=50");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var inList = listDoc.RootElement.GetProperty("items").EnumerateArray()
            .Any(e => e.GetProperty("id").GetGuid() == alertId && e.GetProperty("isAcknowledged").GetBoolean());
        Assert.True(inList);

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
    }

    [Fact]
    public async Task J_K_L_Alert_generation_list_ordering_and_compact_vs_detail_shape_remain_stable()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ack-jkl@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-J",
            ruleType = "SignalTypeEquals",
            matchValue = "t-j",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-j",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var before = await client.GetAsync("/alerts?page=1&pageSize=10");
        before.EnsureSuccessStatusCode();
        using var beforeDoc = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        var alertId = beforeDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        var totalBefore = beforeDoc.RootElement.GetProperty("totalCount").GetInt32();

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var after = await client.GetAsync("/alerts?page=1&pageSize=10");
        after.EnsureSuccessStatusCode();
        using var afterDoc = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.Equal(totalBefore, afterDoc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertId, afterDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());

        var item0 = afterDoc.RootElement.GetProperty("items")[0];
        Assert.False(item0.TryGetProperty("rule", out _));
        Assert.False(item0.TryGetProperty("signal", out _));

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("R-J", d.RootElement.GetProperty("rule").GetProperty("name").GetString());
        Assert.Equal("t-j", d.RootElement.GetProperty("signal").GetProperty("type").GetString());
    }
}
