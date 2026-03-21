using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

/// <summary>
/// Verifies EF-backed repositories persist data across HTTP requests (Testing uses in-memory EF DB).
/// </summary>
public sealed class PersistenceApiTests
{
    [Fact]
    public async Task Get_signals_returns_accumulated_signals_after_multiple_ingests()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "persist@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/signals", new
        {
            source = "a",
            type = "t1",
            timestampUtc = DateTimeOffset.UtcNow
        });
        await client.PostAsJsonAsync("/signals", new
        {
            source = "b",
            type = "t2",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var listResponse = await client.GetAsync("/signals");
        listResponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());

        Assert.Equal(2, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Create_rule_ingest_matching_signal_persisted_alert_visible_on_list()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "persist2@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "Eq",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });
        ruleResponse.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/signals", new
        {
            source = "s",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        Assert.True(alertsDoc.RootElement.GetProperty("items").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Post_rules_invalid_threshold_returns_400_and_does_not_block_subsequent_valid_rule()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var bad = await client.PostAsJsonAsync("/rules", new
        {
            name = "Bad",
            ruleType = "SignalValueGreaterThan",
            matchValue = "not-a-number",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await client.PostAsJsonAsync("/rules", new
        {
            name = "Good",
            ruleType = "SignalTypeEquals",
            matchValue = "ok",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
    }
}
