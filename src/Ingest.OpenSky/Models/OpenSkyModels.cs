using System.Text.Json.Serialization;

namespace Ingest.OpenSky.Models;

public class OpenSkyResponse
{
    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("states")]
    public object[][]? States { get; set; }
}

public class OpenSkyState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string? OriginCountry { get; set; }
    public long? TimePosition { get; set; }
    public long? LastContact { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public bool? OnGround { get; set; }
    public double? Velocity { get; set; }
    public double? TrueTrack { get; set; }
    public double? VerticalRate { get; set; }
    public int[]? Sensors { get; set; }
    public double? GeoAltitude { get; set; }
    public string? Squawk { get; set; }
    public bool? Spi { get; set; }
    public int? PositionSource { get; set; }

    public static OpenSkyState? FromArray(object[] state)
    {
        if (state == null || state.Length < 17) return null;

        try
        {
            return new OpenSkyState
            {
                Icao24 = state[0]?.ToString()?.Trim() ?? string.Empty,
                Callsign = state[1]?.ToString()?.Trim(),
                OriginCountry = state[2]?.ToString()?.Trim(),
                TimePosition = state[3] as long?,
                LastContact = state[4] as long?,
                Longitude = Convert.ToDouble(state[5]),
                Latitude = Convert.ToDouble(state[6]),
                BaroAltitude = state[7] != null ? Convert.ToDouble(state[7]) : null,
                OnGround = state[8] as bool?,
                Velocity = state[9] != null ? Convert.ToDouble(state[9]) : null,
                TrueTrack = state[10] != null ? Convert.ToDouble(state[10]) : null,
                VerticalRate = state[11] != null ? Convert.ToDouble(state[11]) : null,
                GeoAltitude = state[13] != null ? Convert.ToDouble(state[13]) : null,
                Squawk = state[14]?.ToString(),
                Spi = state[15] as bool?,
                PositionSource = state[16] as int?
            };
        }
        catch
        {
            return null;
        }
    }
}