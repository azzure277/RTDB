namespace Shared.Infrastructure
{
    using System.Text.Json;
    using StackExchange.Redis;
    using Contracts;

    public interface ITrafficRepository
    {
        Task<StateDto> GetStateAsync(string airport, CancellationToken ct);
        Task UpsertPositionsAsync(string airport, IReadOnlyList<PositionDto> positions, CancellationToken ct);
        Task SetSequenceAsync(string airport, IReadOnlyList<string> ids, CancellationToken ct);
    }

    public sealed class RedisTrafficRepository(IConnectionMultiplexer mux) : ITrafficRepository
    {
        readonly IDatabase _db = mux.GetDatabase();
        static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);


        public async Task<StateDto> GetStateAsync(string airport, CancellationToken ct)
        {
            var p = await _db.StringGetAsync($"positions:{airport}");
            var s = await _db.StringGetAsync($"sequence:{airport}");

            var positions = p.HasValue
                ? JsonSerializer.Deserialize<List<PositionDto>>(p!, _json) ?? new()
                : new();

            var seq = s.HasValue
                ? JsonSerializer.Deserialize<List<string>>(s!, _json) ?? new()
                : new();

            // DEBUG LOGGING
            Console.WriteLine($"[DEBUG] GetStateAsync for airport={airport}");
            Console.WriteLine($"[DEBUG] positions count: {positions.Count}");
            foreach (var pos in positions)
            {
                Console.WriteLine($"[DEBUG] Position: Id={pos.Id}, Icao={pos.Icao}, Lat={pos.Lat}, Lon={pos.Lon}, AltFt={pos.AltFt}, GsKt={pos.GsKt}");
            }
            Console.WriteLine($"[DEBUG] sequence count: {seq.Count}");
            Console.WriteLine($"[DEBUG] sequence: {string.Join(",", seq)}");

            return new StateDto(airport, positions, seq, DateTime.UtcNow);
        }

        public async Task UpsertPositionsAsync(string airport, IReadOnlyList<PositionDto> positions, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(positions, _json);
            await _db.StringSetAsync($"positions:{airport}", payload, expiry: TimeSpan.FromMinutes(5));
            // optional: also write per-aircraft keys if you want quick lookups later
        }

        public async Task SetSequenceAsync(string airport, IReadOnlyList<string> ids, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(ids, _json);
            await _db.StringSetAsync($"sequence:{airport}", payload, expiry: TimeSpan.FromMinutes(5));
        }
    }
}
