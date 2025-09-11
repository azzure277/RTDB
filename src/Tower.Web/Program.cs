
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/state", async (IConnectionMultiplexer mux, IConfiguration cfg) =>
{
    var db = mux.GetDatabase();
    var airport = cfg["Airport:Icao"] ?? "KSFO";
    var geoKey = $"geo:{airport.ToLower()}";

    var centerLon = -122.375; var centerLat = 37.6189; // KSFO 28L
    // 10 NM = 11.5078 miles
    var neighbors = await db.GeoRadiusAsync(
        geoKey, centerLon, centerLat, 11.5078, GeoUnit.Miles,
        count: -1, order: null, options: GeoRadiusOptions.WithCoordinates | GeoRadiusOptions.WithDistance);
    var positions = new List<object>();
    foreach (var n in neighbors)
    {
        var raw = await db.StringGetAsync($"pos:{n.Member}");
        if (raw.IsNullOrEmpty) continue;
    var p = JsonSerializer.Deserialize<PositionEvent>(raw!);
        if (p is null) continue;
        positions.Add(new {
            p.Icao24, p.Flight, p.Lat, p.Lon, p.AltFt, p.GsKt, p.HdgDeg, p.VsFpm, p.FuelKg,
            DistNm = n.Distance,
            Longitude = n.Position?.Longitude,
            Latitude = n.Position?.Latitude
        });
    }

    var seqRaw = await db.StringGetAsync($"seq:{airport}");
    var seq = seqRaw.IsNullOrEmpty ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(seqRaw!) ?? Array.Empty<string>();

    return Results.Json(new { airport, positions, sequence = seq, tsUtc = DateTime.UtcNow }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
});

app.Run();
