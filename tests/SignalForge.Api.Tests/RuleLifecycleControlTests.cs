using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.4: POST activate/deactivate and effect on alert generation.</summary>
public sealed class RuleLifecycleControlTests
{
    [Fact]
    public async Task A_Post_rules_id_activate_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/activate", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task B_Post_rules_id_deactivate_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/deactivate", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task C_Post_rules_id_activate_returns_200_with_jwt_when_rule_exists()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "act@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "A",
            ruleType = "SignalTypeEquals",
            matchValue = "x",
            isActive = false
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"/rules/{id}/activate", new { });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task D_Post_rules_id_deactivate_returns_200_with_jwt_when_rule_exists()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "deact@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "D",
            ruleType = "SignalTypeEquals",
            matchValue = "y",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"/rules/{id}/deactivate", new { });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task E_Post_rules_id_activate_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "act404@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/activate", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task F_Post_rules_id_deactivate_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "deact404@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/rules/{Guid.NewGuid()}/deactivate", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task G_matching_active_rule_creates_alert()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "g@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "G",
            ruleType = "SignalTypeEquals",
            matchValue = "type-g",
            isActive = true
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "type-g",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alerts = await client.GetAsync("/alerts");
        alerts.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await alerts.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task H_deactivated_rule_does_not_create_alert_for_matching_signal()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "h@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "H",
            ruleType = "SignalTypeEquals",
            matchValue = "type-h",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s1",
            type = "type-h",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var a1 = await client.GetAsync("/alerts");
        a1.EnsureSuccessStatusCode();
        using var d1 = JsonDocument.Parse(await a1.Content.ReadAsStringAsync());
        Assert.Equal(1, d1.RootElement.GetProperty("totalCount").GetInt32());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deact = await client.PostAsJsonAsync($"/rules/{ruleId}/deactivate", new { });
        deact.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s2",
            type = "type-h",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var a2 = await client.GetAsync("/alerts");
        a2.EnsureSuccessStatusCode();
        using var d2 = JsonDocument.Parse(await a2.Content.ReadAsStringAsync());
        Assert.Equal(1, d2.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task I_reactivated_rule_resumes_creating_alerts()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "i@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleRes = await client.PostAsJsonAsync("/rules", new
        {
            name = "I",
            ruleType = "SignalTypeEquals",
            matchValue = "type-i",
            isActive = true
        });
        ruleRes.EnsureSuccessStatusCode();
        using var ruleDoc = JsonDocument.Parse(await ruleRes.Content.ReadAsStringAsync());
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s1",
            type = "type-i",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var deact = await client.PostAsJsonAsync($"/rules/{ruleId}/deactivate", new { });
        deact.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s2",
            type = "type-i",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var act = await client.PostAsJsonAsync($"/rules/{ruleId}/activate", new { });
        act.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s3",
            type = "type-i",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alerts = await client.GetAsync("/alerts");
        alerts.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await alerts.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
    }
}
