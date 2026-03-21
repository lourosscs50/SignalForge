using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalForge.Api.Tests;

public sealed class IngestSignalRuleEvaluationTests
{
    [Fact]
    public async Task Post_signals_creates_alert_when_SignalTypeEquals_rule_matches()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "ingest@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "Match temperature",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);
        var ruleJson = await ruleResponse.Content.ReadAsStringAsync();
        using var ruleDoc = JsonDocument.Parse(ruleJson);
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        signalResponse.EnsureSuccessStatusCode();
        var signalJson = await signalResponse.Content.ReadAsStringAsync();
        using var signalDoc = JsonDocument.Parse(signalJson);
        var signalId = signalDoc.RootElement.GetProperty("id").GetGuid();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        var found = false;
        foreach (var el in alertsDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId && el.GetProperty("ruleId").GetGuid() == ruleId)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }

    [Fact]
    public async Task Post_signals_creates_alert_when_SignalTypeContains_rule_matches()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "contains@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "Contains temp",
            ruleType = "SignalTypeContains",
            matchValue = "temp",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);
        var ruleJson = await ruleResponse.Content.ReadAsStringAsync();
        using var ruleDoc = JsonDocument.Parse(ruleJson);
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        signalResponse.EnsureSuccessStatusCode();
        var signalJson = await signalResponse.Content.ReadAsStringAsync();
        using var signalDoc = JsonDocument.Parse(signalJson);
        var signalId = signalDoc.RootElement.GetProperty("id").GetGuid();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        var found = false;
        foreach (var el in alertsDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId && el.GetProperty("ruleId").GetGuid() == ruleId)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }

    [Fact]
    public async Task Post_signals_creates_alert_when_SignalValueGreaterThan_rule_matches()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "threshold@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ruleResponse = await client.PostAsJsonAsync("/rules", new
        {
            name = "Value over 10",
            ruleType = "SignalValueGreaterThan",
            matchValue = "10",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);
        var ruleJson = await ruleResponse.Content.ReadAsStringAsync();
        using var ruleDoc = JsonDocument.Parse(ruleJson);
        var ruleId = ruleDoc.RootElement.GetProperty("id").GetGuid();

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow,
            value = 15.5
        });

        signalResponse.EnsureSuccessStatusCode();
        var signalJson = await signalResponse.Content.ReadAsStringAsync();
        using var signalDoc = JsonDocument.Parse(signalJson);
        var signalId = signalDoc.RootElement.GetProperty("id").GetGuid();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        var found = false;
        foreach (var el in alertsDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (el.GetProperty("signalId").GetGuid() == signalId && el.GetProperty("ruleId").GetGuid() == ruleId)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }

    [Fact]
    public async Task Post_signals_does_not_create_alert_when_types_differ()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "nomatch@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Match temperature",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = true
        });

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "pressure",
            timestampUtc = DateTimeOffset.UtcNow
        });

        signalResponse.EnsureSuccessStatusCode();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        Assert.Equal(0, alertsDoc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Post_signals_does_not_create_alert_when_rule_is_disabled()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "disabled@example.com", "User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/rules", new
        {
            name = "Match temperature",
            ruleType = "SignalTypeEquals",
            matchValue = "temperature",
            isActive = false
        });

        var signalResponse = await client.PostAsJsonAsync("/signals", new
        {
            source = "sensor-1",
            type = "temperature",
            timestampUtc = DateTimeOffset.UtcNow
        });

        signalResponse.EnsureSuccessStatusCode();

        var alertsResponse = await client.GetAsync("/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        using var alertsDoc = JsonDocument.Parse(await alertsResponse.Content.ReadAsStringAsync());

        Assert.Equal(0, alertsDoc.RootElement.GetProperty("items").GetArrayLength());
    }
}
