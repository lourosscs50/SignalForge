using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.7: rule audit write + GET /rules/{id}/audit.</summary>
public sealed class RuleAuditTests
{
    [Fact]
    public async Task A_creating_a_rule_writes_a_Created_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-a@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "A",
            ruleType = "SignalTypeEquals",
            matchValue = "x",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal("Created", arr[0].GetProperty("action").GetString());
        Assert.Equal(id, arr[0].GetProperty("ruleId").GetGuid());
    }

    [Fact]
    public async Task B_activating_a_rule_writes_an_Activated_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-b@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "B",
            ruleType = "SignalTypeEquals",
            matchValue = "y",
            isActive = false
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/activate", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Created", actions);
        Assert.Contains("Activated", actions);
    }

    [Fact]
    public async Task C_deactivating_a_rule_writes_a_Deactivated_audit_entry()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-c@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "C",
            ruleType = "SignalTypeEquals",
            matchValue = "z",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var actions = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToArray();
        Assert.Contains("Created", actions);
        Assert.Contains("Deactivated", actions);
    }

    [Fact]
    public async Task D_Get_rules_id_audit_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task E_Get_rules_id_audit_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-e@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task F_Get_rules_id_audit_returns_audit_entries_for_an_existing_rule()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-f@example.com", "User", "secret");
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

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task G_Get_rules_id_audit_returns_entries_in_OccurredAtUtc_descending_then_Id_ascending()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-g@example.com", "User", "secret");
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

        await Task.Delay(15);
        await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        await Task.Delay(15);
        await client.PostAsJsonAsync($"/rules/{id}/activate", new { });

        var audit = await client.GetAsync($"/rules/{id}/audit");
        audit.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await audit.Content.ReadAsStringAsync());
        var entries = doc.RootElement.EnumerateArray().Select(e => new
        {
            Action = e.GetProperty("action").GetString(),
            Occurred = e.GetProperty("occurredAtUtc").GetDateTimeOffset(),
            EntryId = e.GetProperty("id").GetGuid()
        }).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal(["Activated", "Deactivated", "Created"], entries.Select(x => x.Action));

        for (var i = 0; i < entries.Count - 1; i++)
        {
            var a = entries[i];
            var b = entries[i + 1];
            Assert.True(
                a.Occurred > b.Occurred || (a.Occurred == b.Occurred && a.EntryId.CompareTo(b.EntryId) < 0),
                "Expected OccurredAtUtc descending, with Id ascending as tie-breaker.");
        }
    }

    [Fact]
    public async Task H_existing_create_activate_deactivate_flow_still_works()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "H",
            ruleType = "SignalTypeEquals",
            matchValue = "h",
            isActive = false
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var act = await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        act.EnsureSuccessStatusCode();
        using var actDoc = JsonDocument.Parse(await act.Content.ReadAsStringAsync());
        Assert.True(actDoc.RootElement.GetProperty("isActive").GetBoolean());

        var deact = await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        deact.EnsureSuccessStatusCode();
        using var deactDoc = JsonDocument.Parse(await deact.Content.ReadAsStringAsync());
        Assert.False(deactDoc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task I_Get_rules_id_inspection_behavior_unchanged()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "audit-i@example.com", "User", "secret");
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

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("I", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
    }
}
