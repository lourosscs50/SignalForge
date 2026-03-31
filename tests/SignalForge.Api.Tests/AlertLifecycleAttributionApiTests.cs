using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 5.2: lifecycle mutations record actor attribution; read models expose latest-state fields.</summary>
public sealed class AlertLifecycleAttributionApiTests
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
    public async Task A_Acknowledge_response_includes_acknowledgedByUserId_matching_jwt_sub()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"attr-ack-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var expectedSub = SignalForgeApiTestHelpers.GetJwtSubject(token);

        var alertId = await CreateAlertAsync(client, "ack-attr");
        var res = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.Equal(expectedSub, doc.RootElement.GetProperty("acknowledgedByUserId").GetString());
        Assert.False(doc.RootElement.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task B_Resolve_response_includes_resolvedByUserId_and_list_detail_reflect()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"attr-res-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var expectedSub = SignalForgeApiTestHelpers.GetJwtSubject(token);

        var alertId = await CreateAlertAsync(client, "res-attr");
        var res = await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });
        res.EnsureSuccessStatusCode();
        using var post = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.Equal(expectedSub, post.RootElement.GetProperty("resolvedByUserId").GetString());

        var list = await client.GetAsync("/alerts?pageSize=10");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var row = listDoc.RootElement.GetProperty("items").EnumerateArray().First(e => e.GetProperty("id").GetGuid() == alertId);
        Assert.Equal(expectedSub, row.GetProperty("resolvedByUserId").GetString());

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(expectedSub, d.RootElement.GetProperty("resolvedByUserId").GetString());
    }

    [Fact]
    public async Task C_Reopen_clears_resolved_attribution_sets_reopen_fields_detail_persists_ack_attribution()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"attr-reo-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var sub = SignalForgeApiTestHelpers.GetJwtSubject(token);

        var alertId = await CreateAlertAsync(client, "reopen-attr");
        await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        await client.PostAsJsonAsync($"/alerts/{alertId}/resolve", new { });

        var reopen = await client.PostAsJsonAsync($"/alerts/{alertId}/reopen", new { });
        reopen.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await reopen.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("resolvedByUserId").ValueKind);
        Assert.Equal(sub, doc.RootElement.GetProperty("acknowledgedByUserId").GetString());
        Assert.Equal(sub, doc.RootElement.GetProperty("reopenedByUserId").GetString());
        Assert.NotEqual(JsonValueKind.Null, doc.RootElement.GetProperty("reopenedAtUtc").ValueKind);

        var detail = await client.GetAsync($"/alerts/{alertId}");
        detail.EnsureSuccessStatusCode();
        using var detailDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(sub, detailDoc.RootElement.GetProperty("acknowledgedByUserId").GetString());
        Assert.Equal(JsonValueKind.Null, detailDoc.RootElement.GetProperty("resolvedByUserId").ValueKind);
    }

    [Fact]
    public async Task D_Lifecycle_mutations_without_jwt_still_401()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var alertId = Guid.NewGuid();
        foreach (var path in new[] { "acknowledge", "resolve", "reopen" })
        {
            var r = await client.PostAsJsonAsync($"/alerts/{alertId}/{path}", new { });
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }
    }

    [Fact]
    public async Task E_Valid_jwt_but_missing_actor_context_returns_401_on_acknowledge()
    {
        using var app = new SignalForgeWebAppFactoryMissingActor();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var token = await Auth(client, $"attr-noactor-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var alertId = await CreateAlertAsync(client, "noactor");
        var res = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task F_Repeated_ack_does_not_change_acknowledgedByUserId()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);
        var tokenA = await Auth(client, $"attr-m1-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var alertId = await CreateAlertAsync(client, "idem-ack");
        var first = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        first.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var by1 = d1.RootElement.GetProperty("acknowledgedByUserId").GetString();

        var tokenB = await Auth(client, $"attr-m2-{Guid.NewGuid():N}@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var second = await client.PostAsJsonAsync($"/alerts/{alertId}/acknowledge", new { });
        second.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(by1, d2.RootElement.GetProperty("acknowledgedByUserId").GetString());
    }
}
