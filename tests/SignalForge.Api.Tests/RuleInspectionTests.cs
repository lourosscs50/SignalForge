using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>Phase 3.3.2: authenticated rule list and get-by-id.</summary>
public sealed class RuleInspectionTests
{
    [Fact]
    public async Task A_Get_rules_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/rules");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task B_Get_rules_returns_200_with_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rules-list@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/rules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task C_Get_rules_returns_expected_paged_items()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rules-page@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "PagedRule",
            ruleType = "SignalTypeEquals",
            matchValue = "z",
            isActive = true
        });

        var response = await client.GetAsync("/rules?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
        Assert.Equal("PagedRule", items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task D_Get_rules_id_returns_401_without_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task E_Get_rules_id_returns_200_with_jwt_when_rule_exists()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rules-one@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/rules", new
        {
            name = "Single",
            ruleType = "SignalTypeEquals",
            matchValue = "m",
            isActive = true
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var get = await client.GetAsync($"/rules/{id}");
        get.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Single", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task F_Get_rules_id_returns_404_when_rule_does_not_exist()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "rules-miss@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/rules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
