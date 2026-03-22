using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.0: POST /rules/{id}/unarchive — restore without auto-activate.</summary>
public sealed class RuleUnarchiveTests
{
    [Fact]
    public async Task A_Unarchiving_an_archived_rule_returns_200_and_RuleResponse()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-a@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });

        var unarchive = await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });
        unarchive.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await unarchive.Content.ReadAsStringAsync());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
        Assert.False(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task B_Unarchived_rule_shows_IsArchived_false()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-b@example.com", "User", "secret");
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
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task C_Unarchived_rule_remains_IsActive_false()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-c@example.com", "User", "secret");
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

        var unarchive = await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });
        unarchive.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await unarchive.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task D_Unarchive_writes_Unarchived_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-d@example.com", "User", "secret");
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

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Unarchived", actions);
    }

    [Fact]
    public async Task E_POST_rules_id_unarchive_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/unarchive", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task F_POST_rules_id_unarchive_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/unarchive", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task G_Re_unarchiving_already_unarchived_rule_is_idempotent_no_duplicate_audit()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-g@example.com", "User", "secret");
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
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var unarchivedCount = doc.RootElement.EnumerateArray()
            .Count(e => e.GetProperty("action").GetString() == "Unarchived");
        Assert.Equal(1, unarchivedCount);
    }

    [Fact]
    public async Task H_Unarchived_inactive_rule_does_not_participate_in_evaluation()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "H",
            ruleType = "SignalTypeEquals",
            matchValue = "type-h",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "type-h",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alerts = await client.GetAsync("/alerts");
        alerts.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await alerts.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task I_Unarchived_then_activated_rule_participates_in_evaluation_again()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "I",
            ruleType = "SignalTypeEquals",
            matchValue = "type-i",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });
        await client.PostAsJsonAsync($"/rules/{id}/activate", new { });

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "type-i",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alerts = await client.GetAsync("/alerts");
        alerts.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await alerts.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task J_Restored_rule_visible_in_GET_rules_id()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-j@example.com", "User", "secret");
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
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("J", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }

    [Fact]
    public async Task K_Audit_includes_Archived_and_Unarchived_history()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-k@example.com", "User", "secret");
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
        await client.PostAsJsonAsync($"/rules/{id}/unarchive", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Archived", actions);
        Assert.Contains("Unarchived", actions);
    }

    [Fact]
    public async Task L_Existing_archive_behavior_unchanged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-l@example.com", "User", "secret");
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

        var archive = await client.PostAsJsonAsync($"/rules/{id}/archive", new { });
        archive.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await archive.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isArchived").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task M_Existing_non_archived_lifecycle_stable()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "una-m@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "M",
            ruleType = "SignalTypeEquals",
            matchValue = "m",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var deact = await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        deact.EnsureSuccessStatusCode();
        var act = await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        act.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await act.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isArchived").GetBoolean());
    }
}
