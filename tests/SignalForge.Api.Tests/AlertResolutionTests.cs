using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>POST /alerts/{id}/resolve; resolution does not mutate acknowledgment.</summary>
public sealed class AlertResolutionTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    private static async Task<Guid> CreateAlertAsync(HttpClient client, string typeSuffix)
    {
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = $"t-{typeSuffix}",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = $"t-{typeSuffix}",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 1.0
        });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task A_POST_alerts_id_resolve_returns_200_with_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-a-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "a");

        var response = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task B_C_D_Resolve_sets_IsResolved_and_ResolvedAtUtc_without_acknowledging()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-bcd-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "bcd");

        var res = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isResolved").GetBoolean());
        var resolvedAt = doc.RootElement.GetProperty("resolvedAtUtc").GetDateTimeOffset();
        Assert.NotEqual(default, resolvedAt);
        Assert.False(doc.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("acknowledgedAtUtc").ValueKind);
    }

    [Fact]
    public async Task E_F_Re_resolve_idempotent_preserves_ResolvedAtUtc_and_AcknowledgedAtUtc()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-ef-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "ef");
        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var firstAck = await client.GetAsync($"/alerts/{alertId}");
        firstAck.EnsureSuccessStatusCode();
        using var ackDoc = JsonDocument.Parse(await firstAck.Content.ReadAsStringAsync());
        var ackTs = ackDoc.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset();

        var first = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        first.EnsureSuccessStatusCode();
        using var f1 = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var resTs = f1.RootElement.GetProperty("resolvedAtUtc").GetDateTimeOffset();

        await Task.Delay(50);

        var second = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        second.EnsureSuccessStatusCode();
        using var f2 = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(resTs, f2.RootElement.GetProperty("resolvedAtUtc").GetDateTimeOffset());
        Assert.Equal(ackTs, f2.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task G_POST_alerts_id_resolve_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/alerts/{Guid.NewGuid()}/resolve", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task H_POST_alerts_id_resolve_returns_404_when_alert_missing()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-h-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/alerts/{Guid.NewGuid()}/resolve", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task I_J_GET_alerts_and_detail_include_resolution_fields()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-ij-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "ij");
        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = listDoc.RootElement.GetProperty("items").EnumerateArray()
            .First(e => e.GetProperty("id").GetGuid() == alertId);
        Assert.True(item.GetProperty("isResolved").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("resolvedAtUtc").ValueKind);

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("isResolved").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, d.RootElement.GetProperty("resolvedAtUtc").ValueKind);
    }

    [Fact]
    public async Task K_Enriched_rule_and_signal_remain_after_resolution()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-k-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-K",
            ruleType = "SignalTypeEquals",
            matchValue = "t-k",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "sk",
            type = "t-k",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(ruleId, d.RootElement.GetProperty("rule").GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrEmpty(d.RootElement.GetProperty("signal").GetProperty("type").GetString()));
    }

    [Fact]
    public async Task L_POST_acknowledge_behavior_unchanged_for_unresolved_alert()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-l-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "l");
        var ack = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        ack.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await ack.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task M_And_O_Ingest_still_creates_unresolved_unacked_alert()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-mo-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-mo",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-mo", timestampUtc = DateTimeOffset.UtcNow });

        var get = await client.GetAsync("/alerts?pageSize=5");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var item = doc.RootElement.GetProperty("items")[0];
        Assert.False(item.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(item.GetProperty("isResolved").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("resolvedAtUtc").ValueKind);
    }

    [Fact]
    public async Task N_List_ordering_and_paging_unchanged_with_resolution_fields_present()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-n-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-n",
            isActive = true
        });
        for (var i = 0; i < 2; i++)
            await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-n", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?page=1&pageSize=1");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("items").GetArrayLength());
        Assert.True(doc.RootElement.GetProperty("items")[0].TryGetProperty("isResolved", out _));
    }

    [Fact]
    public async Task P_Q_Ack_then_resolve_preserves_ack_timestamp_and_lists_resolved()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"res-pq-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "pq");
        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var before = await client.GetAsync($"/alerts/{alertId}");
        before.EnsureSuccessStatusCode();
        using var b = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        var ackTs = b.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset();

        await Task.Delay(30);

        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        var after = await client.GetAsync($"/alerts/{alertId}");
        after.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.Equal(ackTs, a.RootElement.GetProperty("acknowledgedAtUtc").GetDateTimeOffset());
        Assert.True(a.RootElement.GetProperty("isResolved").GetBoolean());

        var listed = await client.GetAsync("/alerts?pageSize=20");
        listed.EnsureSuccessStatusCode();
        using var l = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        var row = l.RootElement.GetProperty("items").EnumerateArray().First(e => e.GetProperty("id").GetGuid() == alertId);
        Assert.True(row.GetProperty("isResolved").GetBoolean());
    }
}
