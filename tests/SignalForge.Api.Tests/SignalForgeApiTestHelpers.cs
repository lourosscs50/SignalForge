using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SignalForge.Api.Tests;

internal static class SignalForgeApiTestHelpers
{
    public static HttpClient CreateClient(SignalForgeWebAppFactory app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    public static async Task<string> RegisterAsync(
        HttpClient client,
        string email,
        string displayName,
        string password)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            displayName,
            password
        });

        response.EnsureSuccessStatusCode();
        return await ExtractTokenAsync(response);
    }

    public static async Task<string> ExtractTokenAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        token = token!.Trim();

        Assert.DoesNotContain("Bearer ", token, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, token.Split('.').Length);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token), $"Returned token is not a readable JWT. Raw body: {body}");

        return token;
    }
}
