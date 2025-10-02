
using Shared.Infrastructure;
using Contracts;
using Microsoft.AspNetCore.Builder;

public static class StateEndpoints
{
    public static IEndpointRouteBuilder MapState(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/state", async (string airport, ITrafficRepository repo, CancellationToken ct) =>
        {
            var dto = await repo.GetStateAsync(airport, ct);
            return Results.Ok(dto);
        })
    .WithName("GetState");

        return app;
    }
}
