using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SignalForge.Api.Tests;

/// <summary>
/// Covers durable identity persistence via EF (in-memory DB in Testing; PostgreSQL in Development).
/// </summary>
public sealed class IdentityPersistenceTests
{
    [Fact]
    public async Task Register_then_login_succeeds_using_persisted_credentials()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "persist-user@example.com",
            displayName = "Persist User",
            password = "correct-horse-battery-staple"
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "persist-user@example.com",
            password = "correct-horse-battery-staple"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await SignalForgeApiTestHelpers.ExtractTokenAsync(login);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Post_auth_register_duplicate_email_returns_400_or_409()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var first = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "dup@example.com",
            displayName = "First",
            password = "secret"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "dup@example.com",
            displayName = "Second",
            password = "other-secret"
        });

        Assert.True(
            second.StatusCode == HttpStatusCode.BadRequest || second.StatusCode == HttpStatusCode.Conflict,
            $"Expected 400 or 409, got {second.StatusCode}.");
    }

    [Fact]
    public async Task Post_auth_login_without_register_returns_401()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "never-registered@example.com",
            password = "any"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_returns_profile_after_register_using_persisted_user_lookup()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "me@example.com", "Me User", "secret");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetAsync("/me");
        me.EnsureSuccessStatusCode();

        var body = await me.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("me@example.com", doc.RootElement.GetProperty("email").GetString());
        Assert.Equal("Me User", doc.RootElement.GetProperty("displayName").GetString());
    }
}
