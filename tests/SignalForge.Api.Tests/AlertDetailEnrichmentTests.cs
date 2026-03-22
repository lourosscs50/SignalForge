using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.1: GET /alerts/{id} enriched rule/signal summaries; list stays compact.</summary>
public sealed class AlertDetailEnrichmentTests
{
    [Fact]
    public async Task A_GET_alerts_id_returns_enriched_rule_summary_fields()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-a@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-A",
            ruleType = "SignalTypeEquals",
            matchValue = "t-a",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-a",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 42.0
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var rule = d.RootElement.GetProperty("rule");
        Assert.Equal(ruleId, rule.GetProperty("id").GetGuid());
        Assert.Equal("R-A", rule.GetProperty("name").GetString());
        Assert.Equal("SignalTypeEquals", rule.GetProperty("ruleType").GetString());
        Assert.Equal("t-a", rule.GetProperty("matchValue").GetString());
        Assert.NotEqual(default, rule.GetProperty("createdAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task B_GET_alerts_id_returns_enriched_signal_summary_fields()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-b@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-B",
            ruleType = "SignalTypeEquals",
            matchValue = "t-b",
            isActive = true
        });

        var sigRes = await client.PostAsJsonAsync("/signals", new
        {
            source = "src-b",
            type = "t-b",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 3.14
        });
        sigRes.EnsureSuccessStatusCode();
        using var sigDoc = JsonDocument.Parse(await sigRes.Content.ReadAsStringAsync());
        var signalId = sigDoc.RootElement.GetProperty("id").GetGuid();

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var signal = d.RootElement.GetProperty("signal");
        Assert.Equal(signalId, signal.GetProperty("id").GetGuid());
        Assert.Equal("src-b", signal.GetProperty("source").GetString());
        Assert.Equal("t-b", signal.GetProperty("type").GetString());
        Assert.Equal(3.14, signal.GetProperty("value").GetDouble());
        Assert.NotEqual(default, signal.GetProperty("occurredAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task C_GET_alerts_id_reflects_current_rule_IsActive_state()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-c@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-C",
            ruleType = "SignalTypeEquals",
            matchValue = "t-c",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-c",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{ruleId}/deactivate", new { });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.False(d.RootElement.GetProperty("rule").GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task D_GET_alerts_id_reflects_current_rule_IsArchived_state()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-d@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-D",
            ruleType = "SignalTypeEquals",
            matchValue = "t-d",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-d",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{ruleId}/archive", new { });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("rule").GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task E_GET_alerts_id_reflects_current_rule_MatchValue()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-e@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-E",
            ruleType = "SignalTypeEquals",
            matchValue = "mv-e",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "mv-e",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("mv-e", d.RootElement.GetProperty("rule").GetProperty("matchValue").GetString());
    }

    [Fact]
    public async Task F_GET_alerts_remains_compact_without_nested_rule_or_signal()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-F",
            ruleType = "SignalTypeEquals",
            matchValue = "t-f",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-f",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var first = doc.RootElement.GetProperty("items")[0];
        Assert.False(first.TryGetProperty("rule", out _));
        Assert.False(first.TryGetProperty("signal", out _));
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("ruleId", out _));
        Assert.True(first.TryGetProperty("signalId", out _));
        Assert.True(first.TryGetProperty("createdAtUtc", out _));
    }

    [Fact]
    public async Task G_GET_alerts_id_returns_404_when_alert_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/alerts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task H_GET_alerts_list_shape_and_paging_unchanged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-H",
            ruleType = "SignalTypeEquals",
            matchValue = "t-h",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-h",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?page=1&pageSize=10");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("items", out var items));
        Assert.True(doc.RootElement.TryGetProperty("totalCount", out _));
        Assert.True(doc.RootElement.TryGetProperty("page", out _));
        Assert.True(doc.RootElement.TryGetProperty("pageSize", out _));
        Assert.Equal(1, items.GetArrayLength());
    }

    [Fact]
    public async Task I_alert_generation_still_creates_listable_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-I",
            ruleType = "SignalTypeEquals",
            matchValue = "t-i",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "t-i",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task J_rule_updated_after_alert_detail_shows_current_MatchValue()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-j@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-J",
            ruleType = "SignalTypeEquals",
            matchValue = "orig-j",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "orig-j",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PutAsJsonAsync($"/rules/{ruleId}", new { name = "R-J2", matchValue = "new-j" });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("new-j", d.RootElement.GetProperty("rule").GetProperty("matchValue").GetString());
    }

    [Fact]
    public async Task K_rule_archived_after_alert_detail_shows_IsArchived()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ade-k@example.com", "User", "secret");
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
            source = "s",
            type = "t-k",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{ruleId}/archive", new { });

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("rule").GetProperty("isArchived").GetBoolean());
    }
}
