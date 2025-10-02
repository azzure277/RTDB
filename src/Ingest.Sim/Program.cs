using Confluent.Kafka;
using Ingest.Sim.Tracks;
using Shared.Models;
using System.Text.Json;

var bootstrap = Environment.GetEnvironmentVariable("KAFKA") ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("TOPIC") ?? "aircraft.position";
var rateHz = int.TryParse(Environment.GetEnvironmentVariable("RATE_HZ"), out var r) ? r : 5;
var count = int.TryParse(Environment.GetEnvironmentVariable("COUNT"), out var c) ? c : 5;

Console.WriteLine($"Sim -> Kafka: {bootstrap}, topic={topic}, rate={rateHz}Hz, count={count}");

var config = new ProducerConfig { BootstrapServers = bootstrap };
using var producer = new ProducerBuilder<string, string>(config).Build();

var start = DateTime.UtcNow;
var rnd = new Random();

var flights = Enumerable.Range(0, count)
    .Select(i => ($"A{rnd.Next(100000, 999999):X}", $"TEST{i:00}"))
    .ToArray();

while (true)
{
    foreach (var (icao24, flight) in flights)
    {
        foreach (var evt in SfoApproach.Generate(icao24, flight, DateTime.UtcNow, rateHz))
        {
            var json = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(topic, new Message<string, string> { Key = icao24, Value = json });
            await Task.Delay(1000 / rateHz);
        }
    }
}
