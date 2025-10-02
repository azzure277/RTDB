
using Shared.Infrastructure;
using Contracts;
using Microsoft.AspNetCore.Builder;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingest", async (
            string airport,
            List<PositionDto> positions,
            ITrafficRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(airport)) return Results.BadRequest("airport required");
            if (positions.Count == 0) return Results.BadRequest("positions empty");
            // (Optional) validate bounds / sanitize
            await repo.UpsertPositionsAsync(airport, positions, ct);
            return Results.Accepted($"/api/state?airport={airport}");
        })
    .WithName("IngestPositions");

        return app;
    }
}
