using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.6: optional isResolved on GET /alerts; combines with ruleId/signalId/isAcknowledged; paging/count/order.</summary>
public sealed class AlertResolutionFilteringTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    [Fact]
    public async Task A_Get_alerts_isResolved_true_returns_only_resolved_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-a-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-a1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-a2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-a1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-a2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id1 = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id1}/resolve", new { });

        var filtered = await client.GetAsync("/alerts?isResolved=true&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, f.RootElement.GetProperty("totalCount").GetInt32());
        Assert.True(f.RootElement.GetProperty("items")[0].GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task B_Get_alerts_isResolved_false_returns_only_unresolved_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-b-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-b1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-b2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-b1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-b2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id1 = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id1}/resolve", new { });

        var filtered = await client.GetAsync("/alerts?isResolved=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, f.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(f.RootElement.GetProperty("items")[0].GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task C_Get_alerts_without_isResolved_preserves_all_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-c-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-c1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-c2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-c1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-c2", timestampUtc = DateTimeOffset.UtcNow });
        var listResp = await client.GetAsync("/alerts?pageSize=20");
        listResp.EnsureSuccessStatusCode();
        using var before = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        await client.PostAsJsonAsync($"/alerts/{before.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid()}/resolve", new { });

        var all = await client.GetAsync("/alerts?pageSize=20");
        all.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await all.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task D_Get_alerts_ruleId_and_isResolved_combine_with_AND()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-d-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r1 = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-A",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-d1",
            isActive = true
        });
        r1.EnsureSuccessStatusCode();
        using var r1Doc = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        var ruleId1 = r1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-B",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-d2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-d1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-d2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Guid alertForR1 = default;
        foreach (var el in listDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("ruleId").GetGuid() == ruleId1)
                alertForR1 = el.GetProperty("id").GetGuid();
        }

        Assert.NotEqual(default, alertForR1);
        await client.PostAsJsonAsync($"/alerts/{alertForR1}/resolve", new { });

        var unresolvedForR1 = await client.GetAsync($"/alerts?ruleId={ruleId1}&isResolved=false&pageSize=20");
        unresolvedForR1.EnsureSuccessStatusCode();
        using var u = JsonDocument.Parse(await unresolvedForR1.Content.ReadAsStringAsync());
        Assert.Equal(0, u.RootElement.GetProperty("totalCount").GetInt32());

        var resolvedForR1 = await client.GetAsync($"/alerts?ruleId={ruleId1}&isResolved=true&pageSize=20");
        resolvedForR1.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await resolvedForR1.Content.ReadAsStringAsync());
        Assert.Equal(1, a.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertForR1, a.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task E_Get_alerts_signalId_and_isResolved_combine_with_AND()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-e-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-e1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-e2",
            isActive = true
        });

        var s1 = await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-e1", timestampUtc = DateTimeOffset.UtcNow });
        s1.EnsureSuccessStatusCode();
        using var s1Doc = JsonDocument.Parse(await s1.Content.ReadAsStringAsync());
        var signalId1 = s1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-e2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Guid alertForS1 = default;
        foreach (var el in listDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId1)
                alertForS1 = el.GetProperty("id").GetGuid();
        }

        Assert.NotEqual(default, alertForS1);
        await client.PostAsJsonAsync($"/alerts/{alertForS1}/resolve", new { });

        var unresolvedForS1 = await client.GetAsync($"/alerts?signalId={signalId1}&isResolved=false&pageSize=20");
        unresolvedForS1.EnsureSuccessStatusCode();
        using var u = JsonDocument.Parse(await unresolvedForS1.Content.ReadAsStringAsync());
        Assert.Equal(0, u.RootElement.GetProperty("totalCount").GetInt32());

        var resolvedForS1 = await client.GetAsync($"/alerts?signalId={signalId1}&isResolved=true&pageSize=20");
        resolvedForS1.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await resolvedForS1.Content.ReadAsStringAsync());
        Assert.Equal(1, a.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task F_Get_alerts_isAcknowledged_and_isResolved_combine_with_AND()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-f-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-f1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-f2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-f1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-f2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        await client.PostAsJsonAsync($"/alerts/{listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid()}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{listDoc.RootElement.GetProperty("items")[1].GetProperty("id").GetGuid()}/acknowledge", new { });

        var combo = await client.GetAsync("/alerts?isAcknowledged=true&isResolved=false&pageSize=20");
        combo.EnsureSuccessStatusCode();
        using var c = JsonDocument.Parse(await combo.Content.ReadAsStringAsync());
        Assert.Equal(2, c.RootElement.GetProperty("totalCount").GetInt32());
        foreach (var el in c.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.True(el.GetProperty("isAcknowledged").GetBoolean());
            Assert.False(el.GetProperty("isResolved").GetBoolean());
        }
    }

    [Fact]
    public async Task G_Filtered_Get_alerts_preserves_explicit_ordering_newest_first()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-g-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-g",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-g", timestampUtc = DateTimeOffset.UtcNow });
        await Task.Delay(30);
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-g", timestampUtc = DateTimeOffset.UtcNow });

        var filtered = await client.GetAsync("/alerts?isResolved=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var i0 = f.RootElement.GetProperty("items")[0].GetProperty("createdAtUtc").GetDateTimeOffset();
        var i1 = f.RootElement.GetProperty("items")[1].GetProperty("createdAtUtc").GetDateTimeOffset();
        Assert.True(i0 >= i1);
    }

    [Fact]
    public async Task H_And_I_Filtered_paging_and_totalCount_match_filtered_set()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-hi-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-hi",
            isActive = true
        });

        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-hi", timestampUtc = DateTimeOffset.UtcNow });

        var p1 = await client.GetAsync("/alerts?isResolved=false&page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());

        var p2 = await client.GetAsync("/alerts?isResolved=false&page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(3, d2.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task J_K_L_M_Resolve_acknowledge_detail_and_ingest_remain_stable()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-jklm-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-jklm",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-jklm", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var res = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        res.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new { source = "s2", type = "t-arf-jklm", timestampUtc = DateTimeOffset.UtcNow });
        var list2 = await client.GetAsync("/alerts?pageSize=10");
        list2.EnsureSuccessStatusCode();
        using var list2Doc = JsonDocument.Parse(await list2.Content.ReadAsStringAsync());
        var alertId2 = list2Doc.RootElement.GetProperty("items").EnumerateArray()
            .First(e => e.GetProperty("id").GetGuid() != alertId).GetProperty("id").GetGuid();

        var ack = await client.PostAsJsonAsync($"/alerts/{alertId2}/acknowledge", new { });
        ack.EnsureSuccessStatusCode();

        var detail = await client.GetAsync($"/alerts/{alertId2}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(string.IsNullOrEmpty(d.RootElement.GetProperty("rule").GetProperty("name").GetString()));
    }

    [Fact]
    public async Task N_And_O_New_alert_unresolved_by_default_and_resolve_moves_between_sets()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-no-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-no",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-no", timestampUtc = DateTimeOffset.UtcNow });

        var before = await client.GetAsync("/alerts?isResolved=false&pageSize=10");
        before.EnsureSuccessStatusCode();
        using var b = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        Assert.Equal(1, b.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(b.RootElement.GetProperty("items")[0].GetProperty("isResolved").GetBoolean());
        var alertId = b.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        var afterFalse = await client.GetAsync("/alerts?isResolved=false&pageSize=10");
        afterFalse.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await afterFalse.Content.ReadAsStringAsync());
        Assert.Equal(0, f.RootElement.GetProperty("totalCount").GetInt32());

        var afterTrue = await client.GetAsync("/alerts?isResolved=true&pageSize=10");
        afterTrue.EnsureSuccessStatusCode();
        using var t = JsonDocument.Parse(await afterTrue.Content.ReadAsStringAsync());
        Assert.Equal(1, t.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertId, t.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task P_IsAcknowledged_false_and_isResolved_true_returns_empty_set()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"arf-p-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-arf-p",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-arf-p", timestampUtc = DateTimeOffset.UtcNow });

        var r = await client.GetAsync("/alerts?isAcknowledged=false&isResolved=true&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }
}
