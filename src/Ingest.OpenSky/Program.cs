using Confluent.Kafka;
using Ingest.OpenSky;
using Ingest.OpenSky.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
var kafkaBootstrap = Environment.GetEnvironmentVariable("KAFKA") ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("TOPIC") ?? "aircraft.position";
var intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_SECONDS"), out var interval) ? interval : 30;
var region = Environment.GetEnvironmentVariable("REGION") ?? "SFO";

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// HTTP Client for OpenSky API
builder.Services.AddHttpClient<OpenSkyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(provider =>
{
    var config = new ProducerConfig 
    { 
        BootstrapServers = kafkaBootstrap,
        MessageTimeoutMs = 10000,
        RequestTimeoutMs = 5000,
        RetryBackoffMs = 100
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// Services
builder.Services.AddSingleton<OpenSkyService>();
builder.Services.AddSingleton<OpenSkyKafkaPublisher>(provider =>
{
    var producer = provider.GetRequiredService<IProducer<string, string>>();
    var logger = provider.GetRequiredService<ILogger<OpenSkyKafkaPublisher>>();
    return new OpenSkyKafkaPublisher(producer, topic, logger);
});

// Background Worker
builder.Services.AddHostedService<OpenSkyWorker>();

// Build and run
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting OpenSky Ingestion Service");
logger.LogInformation("Kafka: {Kafka}, Topic: {Topic}, Region: {Region}, Interval: {Interval}s", 
    kafkaBootstrap, topic, region, intervalSeconds);

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    Environment.Exit(1);
}
finally
{
    // Cleanup Kafka producer
    var producer = host.Services.GetService<IProducer<string, string>>();
    producer?.Dispose();
}