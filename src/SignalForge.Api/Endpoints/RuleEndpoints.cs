using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.UseCases;
using SignalForge.Contracts.Rules;

namespace SignalForge.Api.Endpoints;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rules");

        group.MapPost("/", async (CreateRuleRequest request, CreateRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return Results.Ok(result);
        });
        group.MapGet("/", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}

