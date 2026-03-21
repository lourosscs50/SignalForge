using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.Queries;
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
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            bool? isActive,
            string? ruleType,
            ListRules.Handler handler,
            CancellationToken ct) =>
        {
            var query = new RuleListQuery(
                page ?? 1,
                pageSize ?? ListQueryNormalization.DefaultPageSize,
                isActive,
                ruleType);
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        });

        return app;
    }
}

