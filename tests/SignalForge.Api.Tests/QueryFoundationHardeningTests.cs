using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.5: paging validation, explicit ordering, auth stability for list endpoints.</summary>
public sealed class QueryFoundationHardeningTests
{
    private static async Task<string> AuthClient(HttpClient client)
    {
        return await SignalForgeApiTestHelpers.RegisterAsync(client, $"{Guid.NewGuid():N}@example.com", "User", "secret");
    }

    [Fact]
    public async Task A_Get_rules_rejects_invalid_page_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/rules?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task B_Get_rules_rejects_invalid_pageSize_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/rules?page=1&pageSize=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task C_Get_rules_rejects_pageSize_above_max_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/rules?page=1&pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task D_Get_signals_rejects_invalid_page_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/signals?page=-1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task E_Get_signals_rejects_invalid_pageSize_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/signals?page=1&pageSize=-5");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task F_Get_signals_rejects_pageSize_above_max_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/signals?page=1&pageSize=500");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task G_Get_alerts_rejects_invalid_page_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/alerts?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task H_Get_alerts_rejects_invalid_pageSize_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/alerts?page=1&pageSize=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task I_Get_alerts_rejects_pageSize_above_max_with_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/alerts?page=1&pageSize=102");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task J_Get_rules_returns_items_newest_CreatedAt_first_with_stable_tie_break()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/rules", new
            {
                name = $"Order{i}",
                ruleType = "SignalTypeEquals",
                matchValue = "x",
                isActive = true
            });
            await Task.Delay(20);
        }

        var list = await client.GetAsync("/rules?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal("Order2", items[0].GetProperty("name").GetString());
        Assert.Equal("Order1", items[1].GetProperty("name").GetString());
        Assert.Equal("Order0", items[2].GetProperty("name").GetString());
    }

    [Fact]
    public async Task K_Get_signals_returns_items_newest_OccurredAt_first_with_stable_tie_break()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var t1 = DateTimeOffset.Parse("2025-01-01T10:00:00Z");
        var t2 = DateTimeOffset.Parse("2025-01-02T10:00:00Z");

        await client.PostAsJsonAsync("/signals", new
        {
            source = "a",
            type = "older",
            timestampUtc = t1
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "b",
            type = "newer",
            timestampUtc = t2
        });

        var list = await client.GetAsync("/signals?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal("newer", items[0].GetProperty("type").GetString());
        Assert.Equal("older", items[1].GetProperty("type").GetString());
    }

    [Fact]
    public async Task L_Get_alerts_returns_items_newest_CreatedAt_first_with_stable_tie_break()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "L",
            ruleType = "SignalTypeEquals",
            matchValue = "t-l",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s1",
            type = "t-l",
            timestampUtc = DateTimeOffset.UtcNow
        });
        await Task.Delay(30);
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s2",
            type = "t-l",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 2);
        var id0 = items[0].GetProperty("id").GetGuid();
        var id1 = items[1].GetProperty("id").GetGuid();
        var t0 = items[0].GetProperty("createdAtUtc").GetDateTimeOffset();
        var t1 = items[1].GetProperty("createdAtUtc").GetDateTimeOffset();
        Assert.True(t0 >= t1);
        if (t0 == t1)
            Assert.True(id0.CompareTo(id1) <= 0);
    }

    [Fact]
    public async Task M_valid_paged_list_requests_still_succeed()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await AuthClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "M",
            ruleType = "SignalTypeEquals",
            matchValue = "m",
            isActive = true
        });

        var rules = await client.GetAsync("/rules?page=1&pageSize=20");
        rules.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "m",
            timestampUtc = DateTimeOffset.UtcNow
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var signals = await client.GetAsync("/signals?page=1&pageSize=50");
        signals.EnsureSuccessStatusCode();

        var alerts = await client.GetAsync("/alerts?page=1&pageSize=100");
        alerts.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task N_secured_list_endpoints_still_require_auth()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/rules")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/signals")).StatusCode);
    }
}
