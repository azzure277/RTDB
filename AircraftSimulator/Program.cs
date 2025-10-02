using StackExchange.Redis;
using Contracts;
using System.Text.Json;

Console.WriteLine("🛩️ Starting ATC Demo Data Simulator...");

var redis = ConnectionMultiplexer.Connect("localhost:6379");
var db = redis.GetDatabase();

// Initial aircraft positions around KSFO
var aircraft = new List<Aircraft>
{
    new("UAL123", "KSFO", 37.6213, -122.3790, 5000, 250, 90, "M"),
    new("AAL456", "KSFO", 37.5800, -122.3200, 3000, 180, 270, "L"),
    new("DAL789", "KSFO", 37.6500, -122.4100, 8000, 320, 180, "H"),
    new("SWA234", "KSFO", 37.7000, -122.3500, 4500, 220, 225, "M"),
    new("UAL987", "KSFO", 37.5500, -122.4000, 6000, 280, 135, "M")
};

var random = new Random();

Console.WriteLine($"✅ Simulating {aircraft.Count} aircraft around KSFO");
Console.WriteLine("📊 Real-time position updates every 2 seconds");
Console.WriteLine("🔄 Press Ctrl+C to stop\n");

while (true)
{
    try
    {
        // Update aircraft positions
        foreach (var plane in aircraft)
        {
            // Simulate aircraft movement
            plane.UpdatePosition(random);
        }

        // Convert to DTOs
        var positions = aircraft.Select(a => new PositionDto(
            Id: a.CallSign,
            Icao: a.Icao24,
            Lat: a.Latitude,
            Lon: a.Longitude,
            AltFt: a.Altitude,
            GsKt: a.Speed,
            HeadingDeg: a.Heading,
            TsUtc: DateTime.UtcNow,
            WakeCat: a.WakeCategory
        )).ToList();

        // Update landing sequence (sort by distance to airport)
        var sfoLat = 37.6213;
        var sfoLon = -122.3790;
        var sequence = positions
            .Select(p => new { 
                CallSign = p.Id, 
                Distance = Utils.CalculateDistance(p.Lat, p.Lon, sfoLat, sfoLon) 
            })
            .Where(x => x.Distance < 20) // Within 20nm
            .OrderBy(x => x.Distance)
            .Select(x => x.CallSign)
            .ToList();

        // Store in Redis
        var positionsJson = JsonSerializer.Serialize(positions);
        var sequenceJson = JsonSerializer.Serialize(sequence);

        await db.StringSetAsync("positions:KSFO", positionsJson, TimeSpan.FromMinutes(5));
        await db.StringSetAsync("sequence:KSFO", sequenceJson, TimeSpan.FromMinutes(5));

        Console.WriteLine($"⬆️  Updated {positions.Count} aircraft positions | Landing queue: {sequence.Count} aircraft");

        await Task.Delay(2000); // Update every 2 seconds
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        await Task.Delay(5000);
    }
}

// Helper classes and methods
public class Aircraft
{
    public string CallSign { get; }
    public string Icao24 { get; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int Altitude { get; private set; }
    public int Speed { get; private set; }
    public int Heading { get; private set; }
    public string WakeCategory { get; }
    
    private double _targetLat;
    private double _targetLon;
    private int _targetAlt;

    public Aircraft(string callSign, string icao24, double lat, double lon, int alt, int speed, int heading, string wake)
    {
        CallSign = callSign;
        Icao24 = icao24;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        Speed = speed;
        Heading = heading;
        WakeCategory = wake;
        
        // Set initial targets
        _targetLat = lat;
        _targetLon = lon;
        _targetAlt = alt;
    }

    public void UpdatePosition(Random random)
    {
        // Simulate approach to SFO
        var sfoLat = 37.6213;
        var sfoLon = -122.3790;
        
        // Move towards SFO gradually
        var deltaLat = (sfoLat - Latitude) * 0.002; // Slow movement
        var deltaLon = (sfoLon - Longitude) * 0.002;
        
        Latitude += deltaLat + (random.NextDouble() - 0.5) * 0.001; // Add some randomness
        Longitude += deltaLon + (random.NextDouble() - 0.5) * 0.001;
        
        // Gradual altitude decrease for landing approach
        if (Altitude > 1000)
        {
            Altitude -= random.Next(10, 50);
        }
        
        // Speed changes
        Speed += random.Next(-5, 5);
        Speed = Math.Clamp(Speed, 120, 350);
        
        // Heading towards airport
        Heading = (int)((Math.Atan2(sfoLon - Longitude, sfoLat - Latitude) * 180 / Math.PI) + 360) % 360;
    }
}

public static class Utils
{
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for distance in nautical miles
        var R = 3440.065; // Radius of Earth in nautical miles
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}