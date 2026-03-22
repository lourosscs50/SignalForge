using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.6: optional isActive on GET /rules; ruleId/signalId on GET /alerts.</summary>
public sealed class OperationalFilteringTests
{
    private static async Task<string> Auth(HttpClient client, string email)
    {
        return await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");
    }

    [Fact]
    public async Task A_Get_rules_isActive_true_returns_only_active_rules()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"a-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Active",
            ruleType = "SignalTypeEquals",
            matchValue = "a",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "Inactive",
            ruleType = "SignalTypeEquals",
            matchValue = "b",
            isActive = false
        });

        var response = await client.GetAsync("/rules?isActive=true&pageSize=50");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.True(doc.RootElement.GetProperty("items")[0].GetProperty("isActive").GetBoolean());
        Assert.Equal("Active", doc.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task B_Get_rules_isActive_false_returns_only_inactive_rules()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"b-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "On",
            ruleType = "SignalTypeEquals",
            matchValue = "x",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "Off",
            ruleType = "SignalTypeEquals",
            matchValue = "y",
            isActive = false
        });

        var response = await client.GetAsync("/rules?isActive=false&pageSize=50");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(doc.RootElement.GetProperty("items")[0].GetProperty("isActive").GetBoolean());
        Assert.Equal("Off", doc.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task C_Get_rules_without_isActive_includes_all_rules()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"c-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "C1",
            ruleType = "SignalTypeEquals",
            matchValue = "m",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "C2",
            ruleType = "SignalTypeEquals",
            matchValue = "n",
            isActive = false
        });

        var response = await client.GetAsync("/rules?pageSize=50");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task D_Get_rules_with_isActive_filter_still_requires_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/rules?isActive=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task E_Get_alerts_ruleId_returns_only_alerts_for_that_rule()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"e-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r1 = await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "e1",
            isActive = true
        });
        r1.EnsureSuccessStatusCode();
        using var r1d = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        var ruleId1 = r1d.RootElement.GetProperty("id").GetGuid();

        var r2 = await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "e2",
            isActive = true
        });
        r2.EnsureSuccessStatusCode();
        using var r2d = JsonDocument.Parse(await r2.Content.ReadAsStringAsync());
        var ruleId2 = r2d.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "e1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "e2", timestampUtc = DateTimeOffset.UtcNow });

        var filtered = await client.GetAsync($"/alerts?ruleId={ruleId1}&pageSize=50");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(ruleId1, doc.RootElement.GetProperty("items")[0].GetProperty("ruleId").GetGuid());
    }

    [Fact]
    public async Task F_Get_alerts_signalId_returns_only_alerts_for_that_signal()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"f-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "RF",
            ruleType = "SignalTypeEquals",
            matchValue = "f",
            isActive = true
        });

        var s1 = await client.PostAsJsonAsync("/signals", new
        {
            source = "a",
            type = "f",
            timestampUtc = DateTimeOffset.UtcNow
        });
        s1.EnsureSuccessStatusCode();
        using var s1d = JsonDocument.Parse(await s1.Content.ReadAsStringAsync());
        var signalId1 = s1d.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "b",
            type = "f",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var filtered = await client.GetAsync($"/alerts?signalId={signalId1}&pageSize=50");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(signalId1, doc.RootElement.GetProperty("items")[0].GetProperty("signalId").GetGuid());
    }

    [Fact]
    public async Task G_Get_alerts_ruleId_and_signalId_returns_only_alerts_matching_both()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"g-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "G1",
            ruleType = "SignalTypeEquals",
            matchValue = "g1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "G2",
            ruleType = "SignalTypeEquals",
            matchValue = "g2",
            isActive = true
        });

        var sig = await client.PostAsJsonAsync("/signals", new
        {
            source = "sg",
            type = "g1",
            timestampUtc = DateTimeOffset.UtcNow
        });
        sig.EnsureSuccessStatusCode();
        using var sigDoc = JsonDocument.Parse(await sig.Content.ReadAsStringAsync());
        var signalId = sigDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "sg",
            type = "g2",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var list = await client.GetAsync("/alerts?pageSize=50");
        list.EnsureSuccessStatusCode();
        using var all = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(2, all.RootElement.GetProperty("totalCount").GetInt32());

        var target = all.RootElement.GetProperty("items").EnumerateArray()
            .First(el => el.GetProperty("signalId").GetGuid() == signalId);
        var ruleId = target.GetProperty("ruleId").GetGuid();

        var filtered = await client.GetAsync($"/alerts?ruleId={ruleId}&signalId={signalId}&pageSize=50");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(ruleId, doc.RootElement.GetProperty("items")[0].GetProperty("ruleId").GetGuid());
        Assert.Equal(signalId, doc.RootElement.GetProperty("items")[0].GetProperty("signalId").GetGuid());
    }

    [Fact]
    public async Task H_Get_alerts_without_filters_lists_all_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"h-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "H",
            ruleType = "SignalTypeEquals",
            matchValue = "h",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "h", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "h", timestampUtc = DateTimeOffset.UtcNow });

        var response = await client.GetAsync("/alerts?pageSize=50");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task I_Filtered_Get_rules_preserves_ordering_and_paging()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"i-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/rules", new
            {
                name = $"I{i}",
                ruleType = "SignalTypeEquals",
                matchValue = "z",
                isActive = true
            });
            await Task.Delay(15);
        }

        var p1 = await client.GetAsync("/rules?isActive=true&page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal("I2", d1.RootElement.GetProperty("items")[0].GetProperty("name").GetString());

        var p2 = await client.GetAsync("/rules?isActive=true&page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal("I0", d2.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task J_Filtered_Get_alerts_preserves_ordering_and_paging()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"j-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rule = await client.PostAsJsonAsync("/rules", new
        {
            name = "J",
            ruleType = "SignalTypeEquals",
            matchValue = "j",
            isActive = true
        });
        rule.EnsureSuccessStatusCode();
        using var rd = JsonDocument.Parse(await rule.Content.ReadAsStringAsync());
        var ruleId = rd.RootElement.GetProperty("id").GetGuid();

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/signals", new
            {
                source = "s",
                type = "j",
                timestampUtc = DateTimeOffset.UtcNow
            });
            await Task.Delay(15);
        }

        var p1 = await client.GetAsync($"/alerts?ruleId={ruleId}&page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());

        var p2 = await client.GetAsync($"/alerts?ruleId={ruleId}&page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
    }
}
