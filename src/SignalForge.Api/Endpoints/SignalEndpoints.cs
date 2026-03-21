using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
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
        group.MapGet("/", async (ListSignals.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result);
        });
        group.MapGet("/{id:guid}", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}

