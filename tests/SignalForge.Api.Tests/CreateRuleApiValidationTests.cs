using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

public sealed class CreateRuleApiValidationTests
{
    [Fact]
    public async Task Post_rules_with_invalid_threshold_MatchValue_returns_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/rules", new
        {
            name = "Bad",
            ruleType = "SignalValueGreaterThan",
            matchValue = "abc",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_rules_with_blank_matchValue_returns_400()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/rules", new
        {
            name = "Rule",
            ruleType = "SignalTypeEquals",
            matchValue = "   ",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_rules_with_valid_rule_returns_200()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/rules", new
        {
            name = "OK",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("temperature", doc.RootElement.GetProperty("matchValue").GetString());
    }
}
