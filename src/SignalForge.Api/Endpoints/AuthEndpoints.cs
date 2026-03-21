using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SignalForge.Application.UseCases.Identity;
using SignalForge.Contracts.Identity;

namespace SignalForge.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            RegisterUser.Handler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            LoginUser.Handler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        });

        app.MapGet("/me", [Authorize] async (
            ClaimsPrincipal user,
            GetCurrentUser.Handler handler,
            CancellationToken ct) =>
        {
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var userId))
                return Results.Unauthorized();

            var response = await handler.HandleAsync(new GetCurrentUser.Request(userId), ct);
            return Results.Ok(response);
        });

        return app;
    }
}

