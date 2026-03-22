using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SignalForge.Domain;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.8: PUT /rules/{id} limited name/match editing.</summary>
public sealed class RuleUpdateTests
{
    [Fact]
    public async Task A_PUT_rules_id_returns_200_and_updated_RuleResponse()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-a@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "Before",
            ruleType = "SignalTypeEquals",
            matchValue = "t1",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "After", matchValue = "t2" });
        put.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal("After", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("t2", doc.RootElement.GetProperty("matchValue").GetString());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task B_updating_preserves_IsActive()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-b@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "B",
            ruleType = "SignalTypeContains",
            matchValue = "sub",
            isActive = false
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "B2", matchValue = "sub2" });
        put.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task C_updating_preserves_CreatedAtUtc()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-c@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "C",
            ruleType = "SignalTypeEquals",
            matchValue = "c",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        var createdAt = created.RootElement.GetProperty("createdAtUtc").GetDateTimeOffset();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "C2", matchValue = "c2" });
        put.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal(createdAt, doc.RootElement.GetProperty("createdAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task D_updating_writes_Updated_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-d@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "D",
            ruleType = "SignalTypeEquals",
            matchValue = "d",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "D2", matchValue = "d2" });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Created", actions);
        Assert.Contains("Updated", actions);
    }

    [Fact]
    public async Task E_PUT_rules_id_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PutAsJsonAsync($"/rules/{Guid.NewGuid()}", new { name = "x", matchValue = "y" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task F_PUT_rules_id_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/rules/{Guid.NewGuid()}", new { name = "x", matchValue = "y" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task G_PUT_rejects_empty_Name()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-g@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "G",
            ruleType = "SignalTypeEquals",
            matchValue = "g",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"/rules/{id}", new { name = "   ", matchValue = "g2" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task H_PUT_rejects_empty_MatchValue()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "H",
            ruleType = "SignalTypeEquals",
            matchValue = "h",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"/rules/{id}", new { name = "H2", matchValue = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task I_PUT_rejects_invalid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "I",
            ruleType = "SignalValueGreaterThan",
            matchValue = "10",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"/rules/{id}", new { name = "I2", matchValue = "bad" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task J_PUT_accepts_valid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-j@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "J",
            ruleType = "SignalValueGreaterThan",
            matchValue = "10",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "J2", matchValue = "99.25" });
        put.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal("99.25", doc.RootElement.GetProperty("matchValue").GetString());
    }

    [Fact]
    public async Task K_activate_deactivate_still_works_after_update()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-k@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "K",
            ruleType = "SignalTypeEquals",
            matchValue = "k",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "K2", matchValue = "k2" });

        var deact = await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        deact.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await deact.Content.ReadAsStringAsync());
        Assert.False(d.RootElement.GetProperty("isActive").GetBoolean());

        var act = await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        act.EnsureSuccessStatusCode();
        using var a = JsonDocument.Parse(await act.Content.ReadAsStringAsync());
        Assert.True(a.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task L_rule_inspection_reflects_updated_values()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "upd-l@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "L",
            ruleType = "SignalTypeEquals",
            matchValue = "old",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "LNew", matchValue = "new" });

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("LNew", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("new", doc.RootElement.GetProperty("matchValue").GetString());

        var list = await client.GetAsync("/rules?page=1&pageSize=20");
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = listDoc.RootElement.GetProperty("items");
        var found = false;
        foreach (var item in items.EnumerateArray())
        {
            if (item.GetProperty("id").GetGuid() == id)
            {
                Assert.Equal("LNew", item.GetProperty("name").GetString());
                Assert.Equal("new", item.GetProperty("matchValue").GetString());
                found = true;
                break;
            }
        }

        Assert.True(found);
    }

    [Fact]
    public async Task M_create_behavior_unchanged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "M",
            ruleType = "SignalTypeEquals",
            matchValue = "m",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        Assert.Equal("M", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(RuleTypes.SignalTypeEquals, doc.RootElement.GetProperty("ruleType").GetString());
    }
}
