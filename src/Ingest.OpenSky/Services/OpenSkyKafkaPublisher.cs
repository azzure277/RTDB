using Confluent.Kafka;
using Ingest.OpenSky.Models;
using Shared.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ingest.OpenSky.Services;

public class OpenSkyKafkaPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<OpenSkyKafkaPublisher> _logger;

    public OpenSkyKafkaPublisher(IProducer<string, string> producer, string topic, ILogger<OpenSkyKafkaPublisher> logger)
    {
        _producer = producer;
        _topic = topic;
        _logger = logger;
    }

    public async Task PublishAsync(OpenSkyState state, CancellationToken cancellationToken = default)
    {
        try
        {
            var positionEvent = ConvertToPositionEvent(state);
            var json = JsonSerializer.Serialize(positionEvent);

            await _producer.ProduceAsync(_topic, new Message<string, string>
            {
                Key = state.Icao24,
                Value = json
            }, cancellationToken);

            _logger.LogDebug("Published position for aircraft {Icao24} ({Callsign})", 
                state.Icao24, state.Callsign ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing position for aircraft {Icao24}", state.Icao24);
        }
    }

    public async Task PublishBatchAsync(IEnumerable<OpenSkyState> states, CancellationToken cancellationToken = default)
    {
        var tasks = states.Select(state => PublishAsync(state, cancellationToken));
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Published batch of {Count} aircraft positions", states.Count());
    }

    private static PositionEvent ConvertToPositionEvent(OpenSkyState state)
    {
        // Convert OpenSky data to our internal PositionEvent format
        var altitudeFt = state.BaroAltitude.HasValue 
            ? (int)(state.BaroAltitude.Value * 3.28084) // Convert meters to feet
            : 0;

        var groundSpeedKt = state.Velocity.HasValue 
            ? (int)(state.Velocity.Value * 1.94384) // Convert m/s to knots
            : 0;

        var headingDeg = state.TrueTrack.HasValue 
            ? (int)state.TrueTrack.Value 
            : 0;

        var verticalSpeedFpm = state.VerticalRate.HasValue 
            ? (int)(state.VerticalRate.Value * 196.85) // Convert m/s to ft/min
            : 0;

        // Estimate fuel based on aircraft type (simplified)
        var estimatedFuelKg = EstimateFuel(altitudeFt, groundSpeedKt);

        return new PositionEvent(
            Icao24: state.Icao24.ToUpper(),
            Flight: state.Callsign?.Trim() ?? $"UNK{state.Icao24[^4..]}",
            Lat: state.Latitude!.Value,
            Lon: state.Longitude!.Value,
            AltFt: altitudeFt,
            GsKt: groundSpeedKt,
            HdgDeg: headingDeg,
            VsFpm: verticalSpeedFpm,
            FuelKg: estimatedFuelKg,
            TsUtc: DateTime.UtcNow
        );
    }

    private static int EstimateFuel(int altitudeFt, int groundSpeedKt)
    {
        // Very rough fuel estimation based on altitude and speed
        // This is just for simulation purposes
        if (altitudeFt > 35000 && groundSpeedKt > 400)
            return Random.Shared.Next(15000, 25000); // Heavy aircraft
        else if (altitudeFt > 25000 && groundSpeedKt > 300)
            return Random.Shared.Next(8000, 15000); // Medium aircraft
        else
            return Random.Shared.Next(2000, 8000); // Light aircraft
    }
}