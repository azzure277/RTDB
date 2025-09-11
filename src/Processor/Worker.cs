using Confluent.Kafka;
using Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _log;
    private readonly IConfiguration _cfg;
    private readonly IConnectionMultiplexer _redis;
    private DateTime _lastSeq = DateTime.MinValue;

    public Worker(ILogger<Worker> log, IConfiguration cfg, IConnectionMultiplexer redis)
    { _log = log; _cfg = cfg; _redis = redis; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _cfg["Kafka:BootstrapServers"]!;
        var topic = _cfg["Kafka:Topic"]!;
        var groupId = _cfg["Kafka:GroupId"]!;
        var airport = _cfg["Airport:Icao"] ?? "KSFO";
        var seqRadiusNm = double.Parse(_cfg["Airport:SequenceRadiusNm"] ?? "10");

        var db = _redis.GetDatabase();
        var geoKey = $"geo:{airport.ToLower()}";

        var c = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(c).Build();
        consumer.Subscribe(topic);
        _log.LogInformation("Processor started: {Bootstrap} topic={Topic}", bootstrap, topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cr = consumer.Consume(stoppingToken);
            if (cr == null) continue;

            var evt = JsonSerializer.Deserialize<PositionEvent>(cr.Message.Value);
            if (evt is null) continue;

            // 1) hot state upsert
            var key = $"pos:{evt.Icao24}";
            await db.StringSetAsync(key, cr.Message.Value, TimeSpan.FromMinutes(2));
            await db.GeoAddAsync(geoKey, evt.Lon, evt.Lat, evt.Icao24);

            // 2) recompute simple sequence every 1s
            if ((DateTime.UtcNow - _lastSeq).TotalSeconds >= 1)
            {
                _lastSeq = DateTime.UtcNow;
                // fetch neighbors within radius; crude distance by GeoRadius
                var centerLon = -122.375; var centerLat = 37.6189; // KSFO 28L approx
                // 1 NM = 1.15078 miles, so use seqRadiusNm * 1.15078
                var milesRadius = seqRadiusNm * 1.15078;
                var neighbors = await db.GeoRadiusAsync(geoKey, centerLon, centerLat, milesRadius, GeoUnit.Miles);
                var list = new List<(string icao, double distNm, int altFt, int gsKt)>();
                foreach (var n in neighbors)
                {
                    var raw = await db.StringGetAsync($"pos:{n.Member}");
                    if (raw.IsNullOrEmpty) continue;
                    var p = JsonSerializer.Deserialize<PositionEvent>(raw!);
                    if (p is null) continue;
                    list.Add((p.Icao24, n.Distance ?? 99, p.AltFt, p.GsKt));
                }
                var ordered = list.OrderBy(x => x.distNm).ThenByDescending(x => x.gsKt).Select(x => x.icao).ToArray();
                await db.StringSetAsync($"seq:{airport}", JsonSerializer.Serialize(ordered), TimeSpan.FromSeconds(5));
            }
        }
    }
}
