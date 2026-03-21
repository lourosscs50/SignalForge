using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>
/// Phase 3: pagination and filtering for rules, signals, alerts.
/// </summary>
public sealed class QueryFoundationTests
{
    [Fact]
    public async Task Get_signals_paged_returns_totalCount_and_sliced_items()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "page@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/signals", new
            {
                source = "src",
                type = "t",
                timestampUtc = DateTimeOffset.UtcNow.AddSeconds(-i)
            });
        }

        var p1 = await client.GetAsync("/signals?page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var doc1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, doc1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, doc1.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(2, doc1.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, doc1.RootElement.GetProperty("items").GetArrayLength());

        var p2 = await client.GetAsync("/signals?page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var doc2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(1, doc2.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Get_signals_filter_by_type_returns_only_matching_rows()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "filter@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/signals", new
        {
            source = "a",
            type = "alpha",
            timestampUtc = DateTimeOffset.UtcNow
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "b",
            type = "beta",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var response = await client.GetAsync("/signals?type=alpha");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal("alpha", doc.RootElement.GetProperty("items")[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task Get_rules_returns_persisted_rule_in_items()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Listed",
            ruleType = "SignalTypeEquals",
            matchValue = "x",
            isActive = true
        });

        var list = await client.GetAsync("/rules");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        var first = doc.RootElement.GetProperty("items")[0];
        Assert.Equal("Listed", first.GetProperty("name").GetString());
        Assert.Equal("SignalTypeEquals", first.GetProperty("ruleType").GetString());
    }

    [Fact]
    public async Task Get_alerts_filter_by_ruleId_returns_matching_alert()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "alertfilter@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "heat",
            isActive = true
        });
        ruleResponse.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleResponse.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "heat",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var filtered = await client.GetAsync($"/alerts?ruleId={ruleId}");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(ruleId, doc.RootElement.GetProperty("items")[0].GetProperty("ruleId").GetGuid());
    }
}
