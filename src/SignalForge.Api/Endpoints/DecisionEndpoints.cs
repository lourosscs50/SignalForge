using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;

namespace SignalForge.Api.Endpoints;

public static class DecisionEndpoints
{
    public static IEndpointRouteBuilder MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/decisions").RequireAuthorization();

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? decisionCategory,
            string? decisionType,
            string? status,
            DateTime? fromOccurredUtc,
            DateTime? toOccurredUtc,
            Guid? correlationId,
            string? traceId,
            Guid? executionId,
            Guid? ruleId,
            string? policyProfileKey,
            ListDecisionVisibility.Handler handler,
            CancellationToken ct) =>
        {
            var query = new DecisionListQuery(
                ListQueryNormalization.ResolvePageOrDefault(page),
                ListQueryNormalization.ResolvePageSizeOrDefault(pageSize),
                decisionCategory,
                decisionType,
                status,
                fromOccurredUtc,
                toOccurredUtc,
                correlationId,
                traceId,
                executionId,
                ruleId,
                policyProfileKey);
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        });

        group.MapGet("/metrics", async (GetDecisionVisibilityMetrics.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, GetDecisionVisibility.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}
