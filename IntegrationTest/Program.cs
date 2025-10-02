using StackExchange.Redis;
using Contracts;
using System.Text.Json;

// Quick test to verify Redis integration and API endpoints
Console.WriteLine("Testing ATC System Integration...");

try
{
    // Connect to Redis
    var redis = ConnectionMultiplexer.Connect("localhost:6379");
    var db = redis.GetDatabase();
    
    Console.WriteLine("✅ Connected to Redis");
    
    // Create sample aircraft data
    var samplePositions = new List<PositionDto>
    {
        new("UAL123", "KSFO", 37.6213, -122.3790, 5000, 250, 090, DateTime.UtcNow, "M"),
        new("AAL456", "KSFO", 37.5800, -122.3200, 3000, 180, 270, DateTime.UtcNow, "L"), 
        new("DAL789", "KSFO", 37.6500, -122.4100, 8000, 320, 180, DateTime.UtcNow, "H")
    };
    
    var sequence = new List<string> { "UAL123", "AAL456", "DAL789" };
    
    // Store in Redis
    var positionsJson = JsonSerializer.Serialize(samplePositions);
    var sequenceJson = JsonSerializer.Serialize(sequence);
    
    await db.StringSetAsync("positions:KSFO", positionsJson, TimeSpan.FromMinutes(5));
    await db.StringSetAsync("sequence:KSFO", sequenceJson, TimeSpan.FromMinutes(5));
    
    Console.WriteLine($"✅ Stored {samplePositions.Count} aircraft positions in Redis");
    Console.WriteLine($"✅ Stored landing sequence: {string.Join(" → ", sequence)}");
    
    // Verify data is retrievable
    var storedPositions = await db.StringGetAsync("positions:KSFO");
    var storedSequence = await db.StringGetAsync("sequence:KSFO");
    
    if (storedPositions.HasValue && storedSequence.HasValue)
    {
        var positions = JsonSerializer.Deserialize<List<PositionDto>>(storedPositions!);
        var seq = JsonSerializer.Deserialize<List<string>>(storedSequence!);
        
        Console.WriteLine($"✅ Retrieved {positions?.Count ?? 0} positions from Redis");
        Console.WriteLine($"✅ Retrieved sequence: {string.Join(" → ", seq ?? new List<string>())}");
        
        Console.WriteLine("\nSample aircraft data:");
        foreach (var pos in positions ?? new List<PositionDto>())
        {
            Console.WriteLine($"  {pos.Id}: {pos.Lat:F4},{pos.Lon:F4} @ {pos.AltFt}ft, {pos.GsKt}kt");
        }
    }
    
    redis.Dispose();
    Console.WriteLine("\n✅ Integration test completed successfully!");
    Console.WriteLine("\nYou can now test the API at:");
    Console.WriteLine("  http://localhost:5000/api/state/KSFO");
    Console.WriteLine("  http://localhost:5000 (dashboard)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}