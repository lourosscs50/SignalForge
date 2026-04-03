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
                ListQueryNormalization.ResolvePageOrDefault(page),
                ListQueryNormalization.ResolvePageSizeOrDefault(pageSize),
                isActive,
                ruleType);
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, GetRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}/audit", async (Guid id, GetRuleAudit.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}/metrics", async (Guid id, GetRuleMetrics.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdateRuleRequest request, UpdateRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/activate", async (Guid id, ActivateRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/archive", async (Guid id, ArchiveRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/unarchive", async (Guid id, UnarchiveRule.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        return app;
    }
}

