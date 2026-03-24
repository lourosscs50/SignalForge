using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>
/// Phase 4.8: lifecycle combination views on GET /alerts (no new query params; existing booleans + ruleId/signalId/time).
/// Lifecycle truth: new = unack+unres; ack+unres = work queue; resolved = ack+res; reopened = ack+unres with ack preserved.
/// </summary>
public sealed class AlertLifecycleQueryCoverageTests
{
    private static async Task<string> Auth(HttpClient client, string email) =>
        await SignalForgeApiTestHelpers.RegisterAsync(client, email, "User", "secret");

    [Fact]
    public async Task A_Unacknowledged_unresolved_returns_only_new_unhandled_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-a-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-a",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-a", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-a", timestampUtc = DateTimeOffset.UtcNow });

        var r = await client.GetAsync("/alerts?isAcknowledged=false&isResolved=false&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
        foreach (var el in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.False(el.GetProperty("isAcknowledged").GetBoolean());
            Assert.False(el.GetProperty("isResolved").GetBoolean());
        }
    }

    [Fact]
    public async Task B_Acknowledged_unresolved_returns_only_acked_open_work()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-b-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-b1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-b2",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-b1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-b2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        await client.PostAsJsonAsync($"/alerts/{listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid()}/acknowledge", new { });

        var r = await client.GetAsync("/alerts?isAcknowledged=true&isResolved=false&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        var item = doc.RootElement.GetProperty("items")[0];
        Assert.True(item.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(item.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task C_Resolved_list_returns_only_resolved_and_all_are_acknowledged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-c-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-c",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-c", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });

        var r = await client.GetAsync("/alerts?isResolved=true&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        foreach (var el in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.True(el.GetProperty("isResolved").GetBoolean());
            Assert.True(el.GetProperty("isAcknowledged").GetBoolean());
        }
    }

    [Fact]
    public async Task D_Reopened_alert_appears_in_acknowledged_unresolved_set()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-d-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-d",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-d", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/reopen", new { });

        var r = await client.GetAsync("/alerts?isAcknowledged=true&isResolved=false&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        var item = doc.RootElement.GetProperty("items")[0];
        Assert.Equal(id, item.GetProperty("id").GetGuid());
        Assert.True(item.GetProperty("isAcknowledged").GetBoolean());
        Assert.False(item.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task E_Unacknowledged_and_resolved_combination_returns_empty_set()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-e-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-e",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-e", timestampUtc = DateTimeOffset.UtcNow });

        var r = await client.GetAsync("/alerts?isAcknowledged=false&isResolved=true&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task F_ruleId_with_lifecycle_combination_filters_correctly()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-f-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r1 = await client.PostAsJsonAsync("/rules", new
        {
            name = "R-A",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-f1",
            isActive = true
        });
        r1.EnsureSuccessStatusCode();
        using var r1Doc = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        var ruleId1 = r1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R-B",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-f2",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-f1", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-f2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Guid alertR1 = default;
        foreach (var el in listDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("ruleId").GetGuid() == ruleId1)
                alertR1 = el.GetProperty("id").GetGuid();
        }

        Assert.NotEqual(default, alertR1);
        await client.PostAsJsonAsync($"/alerts/{alertR1}/acknowledge", new { });

        var filtered = await client.GetAsync(
            $"/alerts?ruleId={ruleId1}&isAcknowledged=true&isResolved=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertR1, doc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task G_signalId_with_lifecycle_combination_filters_correctly()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-g-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R1",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-g1",
            isActive = true
        });
        await client.PostAsJsonAsync("/rules", new
        {
            name = "R2",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-g2",
            isActive = true
        });

        var s1 = await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-g1", timestampUtc = DateTimeOffset.UtcNow });
        s1.EnsureSuccessStatusCode();
        using var s1Doc = JsonDocument.Parse(await s1.Content.ReadAsStringAsync());
        var signalId1 = s1Doc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-g2", timestampUtc = DateTimeOffset.UtcNow });

        var list = await client.GetAsync("/alerts?pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Guid alertS1 = default;
        foreach (var el in listDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId1)
                alertS1 = el.GetProperty("id").GetGuid();
        }

        Assert.NotEqual(default, alertS1);
        await client.PostAsJsonAsync($"/alerts/{alertS1}/acknowledge", new { });

        var filtered = await client.GetAsync(
            $"/alerts?signalId={signalId1}&isAcknowledged=true&isResolved=false&pageSize=20");
        filtered.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(alertS1, doc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task H_And_I_Lifecycle_combination_with_paging_and_totalCount()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-hi-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-hi",
            isActive = true
        });
        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-hi", timestampUtc = DateTimeOffset.UtcNow });

        var p1 = await client.GetAsync("/alerts?isAcknowledged=false&isResolved=false&page=1&pageSize=2");
        p1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await p1.Content.ReadAsStringAsync());
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());

        var p2 = await client.GetAsync("/alerts?isAcknowledged=false&isResolved=false&page=2&pageSize=2");
        p2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync());
        Assert.Equal(3, d2.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task J_Ordering_stable_newest_first_under_lifecycle_combination()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-j-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-j",
            isActive = true
        });

        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-j", timestampUtc = DateTimeOffset.UtcNow });
        await Task.Delay(40);
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-j", timestampUtc = DateTimeOffset.UtcNow });

        var prep = await client.GetAsync("/alerts?pageSize=20");
        prep.EnsureSuccessStatusCode();
        using var prepDoc = JsonDocument.Parse(await prep.Content.ReadAsStringAsync());
        foreach (var el in prepDoc.RootElement.GetProperty("items").EnumerateArray())
            await client.PostAsJsonAsync($"/alerts/{el.GetProperty("id").GetGuid()}/acknowledge", new { });

        var r = await client.GetAsync("/alerts?isAcknowledged=true&isResolved=false&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
        var t0 = doc.RootElement.GetProperty("items")[0].GetProperty("createdAtUtc").GetDateTimeOffset();
        var t1 = doc.RootElement.GetProperty("items")[1].GetProperty("createdAtUtc").GetDateTimeOffset();
        Assert.True(t0 >= t1);
    }

    [Fact]
    public async Task K_Omitted_filters_return_full_unfiltered_list()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-k-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-k",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-k", timestampUtc = DateTimeOffset.UtcNow });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-k", timestampUtc = DateTimeOffset.UtcNow });

        var r = await client.GetAsync("/alerts?pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task L_M_N_O_P_Stability_ack_resolve_reopen_detail_and_generation()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-lmno-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-lmno",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-lmno", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var ack = await client.PostAsJsonAsync($"/alerts/{id}/acknowledge", new { });
        ack.EnsureSuccessStatusCode();

        var res = await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });
        res.EnsureSuccessStatusCode();
        using (var resDoc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()))
            Assert.True(resDoc.RootElement.GetProperty("isResolved").GetBoolean());

        var reopen = await client.PostAsJsonAsync($"/alerts/{id}/reopen", new { });
        reopen.EnsureSuccessStatusCode();
        using (var reDoc = JsonDocument.Parse(await reopen.Content.ReadAsStringAsync()))
            Assert.False(reDoc.RootElement.GetProperty("isResolved").GetBoolean());

        var detail = await client.GetAsync($"/alerts/{id}");
        detail.EnsureSuccessStatusCode();
        using (var det = JsonDocument.Parse(await detail.Content.ReadAsStringAsync()))
            Assert.False(string.IsNullOrEmpty(det.RootElement.GetProperty("rule").GetProperty("name").GetString()));

        var ingest = await client.PostAsJsonAsync("/signals", new { source = "s2", type = "t-lcq-lmno", timestampUtc = DateTimeOffset.UtcNow });
        ingest.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Q_Resolve_reopen_resolve_coherent_in_filtered_lists()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-q-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-q",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-q", timestampUtc = DateTimeOffset.UtcNow });
        var list = await client.GetAsync("/alerts?pageSize=5");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var id = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/reopen", new { });
        await client.PostAsJsonAsync($"/alerts/{id}/resolve", new { });

        var resolved = await client.GetAsync("/alerts?isResolved=true&pageSize=20");
        resolved.EnsureSuccessStatusCode();
        using var resDoc = JsonDocument.Parse(await resolved.Content.ReadAsStringAsync());
        Assert.Equal(1, resDoc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(id, resDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());

        var open = await client.GetAsync("/alerts?isAcknowledged=true&isResolved=false&pageSize=20");
        open.EnsureSuccessStatusCode();
        using var openDoc = JsonDocument.Parse(await open.Content.ReadAsStringAsync());
        Assert.Equal(0, openDoc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task R_Created_bounds_combine_with_lifecycle_filters()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"lcq-r-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "R",
            ruleType = "SignalTypeEquals",
            matchValue = "t-lcq-r",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new { source = "s", type = "t-lcq-r", timestampUtc = DateTimeOffset.UtcNow });

        var from = Uri.EscapeDataString("2000-01-01T00:00:00Z");
        var to = Uri.EscapeDataString("2100-01-01T00:00:00Z");
        var r = await client.GetAsync(
            $"/alerts?fromCreatedUtc={from}&toCreatedUtc={to}&isAcknowledged=false&isResolved=false&pageSize=20");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(doc.RootElement.GetProperty("items")[0].GetProperty("isAcknowledged").GetBoolean());
    }
}
