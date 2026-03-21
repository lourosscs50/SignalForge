using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SignalForge.Api.Tests;

public sealed class AuthAndSignalsTests
{
    [Fact]
    public async Task Post_auth_register_returns_readable_jwt()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            displayName = "Test User",
            password = "secret"
        });

        response.EnsureSuccessStatusCode();

        var token = await SignalForgeApiTestHelpers.ExtractTokenAsync(response);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.NotNull(jwt);
        Assert.False(string.IsNullOrEmpty(jwt.RawHeader));
    }

    [Fact]
    public async Task Post_auth_register_returns_token()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            displayName = "Test User",
            password = "secret"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await SignalForgeApiTestHelpers.ExtractTokenAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Post_auth_login_returns_token()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        await SignalForgeApiTestHelpers.RegisterAsync(client, "test@example.com", "Test User", "secret");

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "test@example.com",
            password = "secret"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var token = await SignalForgeApiTestHelpers.ExtractTokenAsync(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Post_auth_login_with_wrong_password_returns_401()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        await SignalForgeApiTestHelpers.RegisterAsync(client, "test@example.com", "Test User", "secret");

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "test@example.com",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_auth_register_duplicate_email_returns_conflict_or_bad_request()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var firstResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            displayName = "Test User",
            password = "secret"
        });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            displayName = "Another User",
            password = "secret"
        });

        Assert.True(
            secondResponse.StatusCode == HttpStatusCode.BadRequest ||
            secondResponse.StatusCode == HttpStatusCode.Conflict,
            $"Expected 400 or 409, but got {(int)secondResponse.StatusCode} {secondResponse.StatusCode}.");
    }

    [Fact]
    public async Task Get_signals_returns_200_with_bearer_token()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "test@example.com", "Test User", "secret");

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/signals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_signals_without_token_returns_401()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/signals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_returns_200_with_bearer_token()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var token = await SignalForgeApiTestHelpers.RegisterAsync(client, "test@example.com", "Test User", "secret");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_without_token_returns_401()
    {
        using var app = new SignalForgeWebAppFactory();
        var client = SignalForgeApiTestHelpers.CreateClient(app);

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}