using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SignalForge.Application.UseCases;

namespace SignalForge.Api.Endpoints;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/alerts");

        group.MapGet("/", async (ListAlerts.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result);
        });
        group.MapGet("/{id:guid}", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
        group.MapPost("/{id:guid}/acknowledge", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
        group.MapPost("/{id:guid}/resolve", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}

