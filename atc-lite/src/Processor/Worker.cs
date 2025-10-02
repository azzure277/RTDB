using StackExchange.Redis;
using Confluent.Kafka;
using Shared.Models;
using System.Text.Json;
using System.Linq;

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
        double centerLon = -122.375, centerLat = 37.6189; // KSFO 28L approx

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
                // 1 NM = 1.15078 miles, so use seqRadiusNm * 1.15078
                var milesRadius = seqRadiusNm * 1.15078;
                var neighbors = await db.GeoRadiusAsync(geoKey, centerLon, centerLat, milesRadius, GeoUnit.Miles);
                var aircraft = new List<(string icao, double distNm, int altFt, int gsKt, WakeClass wake)>();
                foreach (var n in neighbors)
                {
                    var raw = await db.StringGetAsync($"pos:{n.Member}");
                    if (raw.IsNullOrEmpty) continue;
                    var p = JsonSerializer.Deserialize<PositionEvent>(raw!);
                    if (p is null) continue;
                    var wake = MapWake(p.Icao24); // Deterministic mapping by ICAO24
                    aircraft.Add((p.Icao24, n.Distance ?? 99, p.AltFt, p.GsKt, wake));
                }
                // Order by distance, then speed
                var ordered = aircraft.OrderBy(x => x.distNm).ThenByDescending(x => x.gsKt).ToList();

                var result = new List<string>();
                if (ordered.Count > 0)
                {
                    result.Add(ordered[0].icao);
                    double lastDist = ordered[0].distNm;
                    WakeClass lastWake = ordered[0].wake;
                    for (int i = 1; i < ordered.Count; i++)
                    {
                        var curr = ordered[i];
                        int minNm = WakeMinima.RequiredNm(lastWake, curr.wake);
                        double requiredDist = lastDist + minNm;
                        if (curr.distNm < requiredDist)
                        {
                            result.Add("");
                            lastDist = requiredDist;
                        }
                        else
                        {
                            lastDist = curr.distNm;
                        }
                        result.Add(curr.icao);
                        lastWake = curr.wake;
                    }
                }
                await db.StringSetAsync($"seq:{airport}", JsonSerializer.Serialize(result), TimeSpan.FromSeconds(5));
                // Write all current aircraft positions to 'positions:KSFO' and sequence to 'sequence:KSFO' for Tower.Web compatibility
                var allPositions = new List<Contracts.PositionDto>();
                foreach (var n in neighbors)
                {
                    var raw = await db.StringGetAsync($"pos:{n.Member}");
                    if (raw.IsNullOrEmpty) continue;
                    var p = JsonSerializer.Deserialize<PositionEvent>(raw!);
                    if (p is null) continue;
                    // Map PositionEvent to PositionDto with all frontend-required fields
                    allPositions.Add(new Contracts.PositionDto(
                        Id: p.Icao24,
                        Icao: airport,
                        Flight: p.Flight,
                        Icao24: p.Icao24,
                        Lat: p.Lat,
                        Lon: p.Lon,
                        AltFt: p.AltFt,
                        GsKt: p.GsKt,
                        HeadingDeg: p.HdgDeg,
                        DistNm: n.Distance != null ? (int?)Math.Round(n.Distance.Value, 0) : null,
                        FuelKg: p.FuelKg,
                        TsUtc: p.TsUtc,
                        WakeCat: null
                    ));
                }
                await db.StringSetAsync($"positions:{airport}", JsonSerializer.Serialize(allPositions), TimeSpan.FromSeconds(5));
                await db.StringSetAsync($"sequence:{airport}", JsonSerializer.Serialize(result), TimeSpan.FromSeconds(5));

                // --- Pairwise spacing violation advisory ---
                // We have 'ordered' = ICAOs in landing order
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var leaderIcao = ordered[i].icao;
                    var followerIcao = ordered[i + 1].icao;

                    var rawLead = await db.StringGetAsync($"pos:{leaderIcao}");
                    var rawFollow = await db.StringGetAsync($"pos:{followerIcao}");
                    if (rawLead.IsNullOrEmpty || rawFollow.IsNullOrEmpty) continue;

                    var lead = JsonSerializer.Deserialize<PositionEvent>(rawLead!);
                    var foll = JsonSerializer.Deserialize<PositionEvent>(rawFollow!);
                    if (lead is null || foll is null) continue;

                    // Use Redis GEO distance in miles, convert to NM
                    double? dLeadMiles = await db.GeoDistanceAsync(geoKey, leaderIcao, followerIcao, GeoUnit.Miles);
                    if (dLeadMiles is null) continue;
                    double actualNm = dLeadMiles.Value / 1.15078;
                    var reqNm = WakeMinima.RequiredNm(MapWake(leaderIcao), MapWake(followerIcao));
                    if (actualNm < reqNm)
                    {
                        var streamKey = $"adv:{airport}";
                        await db.StreamAddAsync(streamKey, new[]
                        {
                            new NameValueEntry("type","MIN_SPACING_VIOLATION"),
                            new NameValueEntry("leader", leaderIcao),
                            new NameValueEntry("follower", followerIcao),
                            new NameValueEntry("actual_nm", actualNm.ToString("0.0")),
                            new NameValueEntry("required_nm", reqNm.ToString()),
                            new NameValueEntry("ts_utc", DateTime.UtcNow.ToString("O"))
                        }, maxLength: 1000, useApproximateMaxLength: true);
                    }
                }

                // Notify Tower.Web of sequence update
                // var notifyUrl = _cfg["Notify:Url"] ?? "http://localhost:5000/api/notify";
                // await Notifier.NotifyAsync(notifyUrl, "stateUpdated", result);
            }
        }
    }

    // Deterministic mapping by ICAO24
    private static WakeClass MapWake(string icao24)
    {
        // ~1/6 Light, 3/6 Medium, 2/6 Heavy
        var h = icao24.Aggregate(0, (s, c) => s + c);
        return (h % 6) switch { 0 => WakeClass.Light, 1 or 2 or 3 => WakeClass.Medium, _ => WakeClass.Heavy };
    }
}
