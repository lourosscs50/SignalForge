using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 4.2: Updated audit entries carry previous/new Name and MatchValue.</summary>
public sealed class RuleAuditDetailEnrichmentTests
{
    [Fact]
    public async Task A_successful_update_writes_Updated_audit_with_previous_and_new_values()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-a@example.com", "User", "secret");
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

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "After", matchValue = "t2" });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var updatedEntry = doc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("action").GetString() == "Updated");
        var detail = updatedEntry.GetProperty("updateDetail");
        Assert.Equal("Before", detail.GetProperty("previousName").GetString());
        Assert.Equal("After", detail.GetProperty("newName").GetString());
        Assert.Equal("t1", detail.GetProperty("previousMatchValue").GetString());
        Assert.Equal("t2", detail.GetProperty("newMatchValue").GetString());
    }

    [Fact]
    public async Task B_GET_rules_id_audit_returns_enriched_Updated_detail()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-b@example.com", "User", "secret");
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

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "B2", matchValue = "b2" });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var updated = doc.RootElement.EnumerateArray().First(e => e.GetProperty("action").GetString() == "Updated");
        Assert.True(updated.TryGetProperty("updateDetail", out var ud));
        Assert.Equal("B", ud.GetProperty("previousName").GetString());
        Assert.Equal("B2", ud.GetProperty("newName").GetString());
    }

    [Fact]
    public async Task C_non_update_actions_have_null_update_detail()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-c@example.com", "User", "secret");
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

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var createdEntry = doc.RootElement.EnumerateArray().First(e => e.GetProperty("action").GetString() == "Created");
        Assert.Equal(JsonValueKind.Null, createdEntry.GetProperty("updateDetail").ValueKind);
    }

    [Fact]
    public async Task D_no_op_update_does_not_add_Updated_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-d@example.com", "User", "secret");
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

        await client.PutAsJsonAsync($"/rules/{id}", new { name = "D", matchValue = "d" });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        Assert.DoesNotContain(doc.RootElement.EnumerateArray(), e => e.GetProperty("action").GetString() == "Updated");
    }

    [Fact]
    public async Task E_no_op_update_returns_correct_RuleResponse()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-e@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "E",
            ruleType = "SignalTypeEquals",
            matchValue = "e",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "E", matchValue = "e" });
        put.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal("E", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("e", doc.RootElement.GetProperty("matchValue").GetString());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task F_audit_ordering_descending_OccurredAt_then_Id_unchanged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-f@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "F",
            ruleType = "SignalTypeEquals",
            matchValue = "f",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await Task.Delay(15);
        await client.PutAsJsonAsync($"/rules/{id}", new { name = "F2", matchValue = "f2" });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var entries = doc.RootElement.EnumerateArray().Select(e => new
        {
            Action = e.GetProperty("action").GetString(),
            Occurred = e.GetProperty("occurredAtUtc").GetDateTimeOffset(),
            EntryId = e.GetProperty("id").GetGuid()
        }).ToList();

        Assert.Equal(2, entries.Count);
        Assert.Equal(["Updated", "Created"], entries.Select(x => x.Action));
        for (var i = 0; i < entries.Count - 1; i++)
        {
            var a = entries[i];
            var b = entries[i + 1];
            Assert.True(
                a.Occurred > b.Occurred || (a.Occurred == b.Occurred && a.EntryId.CompareTo(b.EntryId) < 0),
                "Expected OccurredAtUtc descending with Id tie-break.");
        }
    }

    [Fact]
    public async Task G_lifecycle_audit_entries_unchanged_without_update_detail()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-g@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "G",
            ruleType = "SignalTypeEquals",
            matchValue = "g",
            isActive = false
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Created", actions);
        Assert.Contains("Activated", actions);
        Assert.Contains("Deactivated", actions);
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            if (e.GetProperty("action").GetString() != "Updated")
                Assert.Equal(JsonValueKind.Null, e.GetProperty("updateDetail").ValueKind);
        }
    }

    [Fact]
    public async Task H_GET_rules_id_audit_still_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task I_GET_rules_id_audit_still_404_for_missing_rule()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task J_rule_inspection_and_update_lifecycle_intact()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rad-j@example.com", "User", "secret");
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

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();

        var put = await client.PutAsJsonAsync($"/rules/{id}", new { name = "J2", matchValue = "j2" });
        put.EnsureSuccessStatusCode();
        using var putDoc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal("J2", putDoc.RootElement.GetProperty("name").GetString());
    }
}
