using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.9: POST /rules/{id}/archive and archived rule semantics.</summary>
public sealed class RuleArchiveTests
{
    [Fact]
    public async Task A_archiving_an_existing_rule_returns_200_and_RuleResponse()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-a@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "A",
            ruleType = "SignalTypeEquals",
            matchValue = "a",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var archive = await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        archive.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await archive.Content.ReadAsStringAsync());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
        Assert.True(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task B_archived_rule_shows_IsArchived_true()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-b@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "B",
            ruleType = "SignalTypeEquals",
            matchValue = "b",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isArchived").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task C_archiving_writes_Archived_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-c@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Archived", actions);
    }

    [Fact]
    public async Task D_archived_rule_does_not_generate_new_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-d@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "D",
            ruleType = "SignalTypeEquals",
            matchValue = "type-d",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s1",
            type = "type-d",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var a1 = await client.GetAsync("/alerts");
        a1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await a1.Content.ReadAsStringAsync());
        Assert.Equal(1, d1.RootElement.GetProperty("totalCount").GetInt32());

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s2",
            type = "type-d",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var a2 = await client.GetAsync("/alerts");
        a2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await a2.Content.ReadAsStringAsync());
        Assert.Equal(1, d2.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task E_POST_rules_id_archive_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/archive", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task F_POST_rules_id_archive_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/archive", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task G_re_archive_is_idempotent_and_does_not_duplicate_Archived_audit()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-g@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var archivedCount = doc.RootElement.EnumerateArray()
            .Count(e => e.GetProperty("action").GetString() == "Archived");
        Assert.Equal(1, archivedCount);
    }

    [Fact]
    public async Task H_archived_rule_cannot_be_activated()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-h@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var act = await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        Assert.Equal(HttpStatusCode.BadRequest, act.StatusCode);
        using var err = JsonDocument.Parse(await act.Content.ReadAsStringAsync());
        Assert.Contains("archived", err.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I_archived_rule_visible_in_GET_rules_id()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "I",
            ruleType = "SignalTypeEquals",
            matchValue = "i",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("I", doc.RootElement.GetProperty("name").GetString());
        Assert.True(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task J_archived_rule_audit_readable()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-j@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "J",
            ruleType = "SignalTypeEquals",
            matchValue = "j",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task K_GET_rules_includes_isArchived_on_items()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-k@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var list = await client.GetAsync("/rules?pageSize=50");
        list.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var item = doc.RootElement.GetProperty("items").EnumerateArray()
            .First(e => e.GetProperty("id").GetGuid() == id);
        Assert.True(item.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task L_create_update_deactivate_stable_for_non_archived_rules()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-l@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "L",
            ruleType = "SignalTypeEquals",
            matchValue = "l",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "L2", matchValue = "l2" });
        put.EnsureSuccessStatusCode();

        var deact = await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        deact.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await deact.Content.ReadAsStringAsync());
        Assert.False(d.RootElement.GetProperty("isActive").GetBoolean());
        Assert.False(d.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task M_evaluation_still_works_for_non_archived_active_rules()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "arc-m@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "M",
            ruleType = "SignalTypeEquals",
            matchValue = "type-m",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "type-m",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alerts = await client.GetAsync("/alerts");
        alerts.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await alerts.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }
}
