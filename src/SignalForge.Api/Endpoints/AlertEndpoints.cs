using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;

namespace SignalForge.Api.Endpoints;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/alerts");

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            Guid? ruleId,
            Guid? signalId,
            DateTime? fromCreatedUtc,
            DateTime? toCreatedUtc,
            ListAlerts.Handler handler,
            CancellationToken ct) =>
        {
            var query = new AlertListQuery(
                ListQueryNormalization.ResolvePageOrDefault(page),
                ListQueryNormalization.ResolvePageSizeOrDefault(pageSize),
                ruleId,
                signalId,
                fromCreatedUtc,
                toCreatedUtc);
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        });
        group.MapGet("/{id:guid}", async (Guid id, GetAlert.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
        group.MapPost("/{id:guid}/acknowledge", async (Guid id, AcknowledgeAlert.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();
        group.MapPost("/{id:guid}/resolve", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}

