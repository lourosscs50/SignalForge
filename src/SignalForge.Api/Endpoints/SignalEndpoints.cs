using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts.Signals;

namespace SignalForge.Api.Endpoints;

public static class SignalEndpoints
{
    public static IEndpointRouteBuilder MapSignalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/signals").RequireAuthorization();

        group.MapPost("/", async (IngestSignalRequest request, IngestSignal.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return Results.Ok(result);
        });
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? type,
            string? source,
            DateTime? fromOccurredUtc,
            DateTime? toOccurredUtc,
            ListSignals.Handler handler,
            CancellationToken ct) =>
        {
            var query = new SignalListQuery(
                page ?? 1,
                pageSize ?? ListQueryNormalization.DefaultPageSize,
                type,
                source,
                fromOccurredUtc,
                toOccurredUtc);
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        });
        group.MapGet("/{id:guid}", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}

