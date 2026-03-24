using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.4: optional isAcknowledged on GET /alerts; combines with ruleId/signalId; paging/count/order.</summary>
public sealed class AlertOperationalFilteringTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    [Fact]
    public async Task A_Get_alerts_isAcknowledged_true_returns_only_acknowledged_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-a-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-a",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-b",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-a", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-b", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id1 = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id1}/acknowledge", new { });

        var filtered = await client.GetAsync("/alerts?isAcknowledged=true&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, f.RootElement.GetProperty("totalCount").GetInt32());
        Assert.True(f.RootElement.GetProperty("items")[0].GetProperty("isAcknowledged").GetBoolean());
    }

    [Fact]
    public async Task B_Get_alerts_isAcknowledged_false_returns_only_unacknowledged_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-b-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-a",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-b",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-a", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-b", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id1 = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id1}/acknowledge", new { });

        var filtered = await client.GetAsync("/alerts?isAcknowledged=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, f.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(f.RootElement.GetProperty("items")[0].GetProperty("isAcknowledged").GetBoolean());
    }

    [Fact]
    public async Task C_Get_alerts_without_isAcknowledged_preserves_all_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-c-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-a",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-b",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-a", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-b", timestampUtc = DateTimeOffset.UtcNow });
        var listResp = await client.GetAsync("/alerts?pageSize=20");
        listResp.EnsureSuccessStatusCode();
        using var before = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        await client.PostAsJsonAsync($"/alerts/{before.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid()}/acknowledge", new { });

        var all = await client.GetAsync("/alerts?pageSize=20");
        all.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await all.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task D_Get_alerts_ruleId_and_isAcknowledged_combine_with_AND()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-d-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r1 = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-A",
            ruleType = "SignalTypeEquals",
            matchValue = "t-d1",
            isActive = true
        });
        r1.EnsureSuccessStatusCode();
        using var r1Doc = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        var ruleId1 = r1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-B",
            ruleType = "SignalTypeEquals",
            matchValue = "t-d2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-d1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-d2", timestampUtc = DateTimeOffset.UtcNow });

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
        await client.PostAsJsonAsync($"/alerts/{alertForR1}/acknowledge", new { });

        var unackedForR1 = await client.GetAsync($"/alerts?ruleId={ruleId1}&isAcknowledged=false&pageSize=20");
        unackedForR1.EnsureSuccessStatusCode();
        using var u = JsonDocument.Parse(await unackedForR1.Content.ReadAsStringAsync());
        Assert.Equal(0, u.RootElement.GetProperty("totalCount").GetInt32());

        var ackedForR1 = await client.GetAsync($"/alerts?ruleId={ruleId1}&isAcknowledged=true&pageSize=20");
        ackedForR1.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await ackedForR1.Content.ReadAsStringAsync());
        Assert.Equal(1, a.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertForR1, a.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task E_Get_alerts_signalId_and_isAcknowledged_combine_with_AND()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-e-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-e1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-e2",
            isActive = true
        });

        var s1 = await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-e1", timestampUtc = DateTimeOffset.UtcNow });
        s1.EnsureSuccessStatusCode();
        using var s1Doc = JsonDocument.Parse(await s1.Content.ReadAsStringAsync());
        var signalId1 = s1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-e2", timestampUtc = DateTimeOffset.UtcNow });

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
        await client.PostAsJsonAsync($"/alerts/{alertForS1}/acknowledge", new { });

        var unackedForS1 = await client.GetAsync($"/alerts?signalId={signalId1}&isAcknowledged=false&pageSize=20");
        unackedForS1.EnsureSuccessStatusCode();
        using var u = JsonDocument.Parse(await unackedForS1.Content.ReadAsStringAsync());
        Assert.Equal(0, u.RootElement.GetProperty("totalCount").GetInt32());

        var ackedForS1 = await client.GetAsync($"/alerts?signalId={signalId1}&isAcknowledged=true&pageSize=20");
        ackedForS1.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await ackedForS1.Content.ReadAsStringAsync());
        Assert.Equal(1, a.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task F_Filtered_Get_alerts_preserves_explicit_ordering_newest_first()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-f-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-f",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-f", timestampUtc = DateTimeOffset.UtcNow });
        await Task.Delay(30);
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-f", timestampUtc = DateTimeOffset.UtcNow });

        var filtered = await client.GetAsync("/alerts?isAcknowledged=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var i0 = f.RootElement.GetProperty("items")[0].GetProperty("createdAtUtc").GetDateTimeOffset();
        var i1 = f.RootElement.GetProperty("items")[1].GetProperty("createdAtUtc").GetDateTimeOffset();
        Assert.True(i0 >= i1);
    }

    [Fact]
    public async Task G_And_H_Filtered_paging_and_totalCount_match_filtered_set()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-gh-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-gh",
            isActive = true
        });

        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-gh", timestampUtc = DateTimeOffset.UtcNow });

        var p1 = await client.GetAsync("/alerts?isAcknowledged=false&page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());

        var p2 = await client.GetAsync("/alerts?isAcknowledged=false&page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(3, d2.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task I_J_K_Post_acknowledge_and_get_detail_and_ingest_remain_stable()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-ijk-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-ijk",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-ijk", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alertId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var ack = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        ack.EnsureSuccessStatusCode();

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.True(d.RootElement.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(string.IsNullOrEmpty(d.RootElement.GetProperty("rule").GetProperty("name").GetString()));

        var ingest = await client.PostAsJsonAsync("/signals", new { source = "s2", type = "t-ijk", timestampUtc = DateTimeOffset.UtcNow });
        ingest.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task L_And_M_New_alert_is_unacked_and_moves_sets_after_acknowledge()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"aof-lm-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lm",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lm", timestampUtc = DateTimeOffset.UtcNow });

        var beforeAck = await client.GetAsync("/alerts?isAcknowledged=false&pageSize=10");
        beforeAck.EnsureSuccessStatusCode();
        using var b = JsonDocument.Parse(await beforeAck.Content.ReadAsStringAsync());
        Assert.Equal(1, b.RootElement.GetProperty("totalCount").GetInt32());
        var alertId = b.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });

        var afterFalse = await client.GetAsync("/alerts?isAcknowledged=false&pageSize=10");
        afterFalse.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await afterFalse.Content.ReadAsStringAsync());
        Assert.Equal(0, f.RootElement.GetProperty("totalCount").GetInt32());

        var afterTrue = await client.GetAsync("/alerts?isAcknowledged=true&pageSize=10");
        afterTrue.EnsureSuccessStatusCode();
        using var t = JsonDocument.Parse(await afterTrue.Content.ReadAsStringAsync());
        Assert.Equal(1, t.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertId, t.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }
}
