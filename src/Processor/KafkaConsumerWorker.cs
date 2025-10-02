using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure;
using Shared.Models;
using Contracts;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Processor;

public class KafkaConsumerWorker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ITrafficRepository _repository;
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly string _topic;
    private readonly string _airport;
    private readonly ConcurrentDictionary<string, PositionDto> _activePositions = new();

    public KafkaConsumerWorker(
        IConsumer<string, string> consumer,
        ITrafficRepository repository,
        ILogger<KafkaConsumerWorker> logger)
    {
        _consumer = consumer;
        _repository = repository;
        _logger = logger;
        _topic = Environment.GetEnvironmentVariable("TOPIC") ?? "aircraft.position";
        _airport = Environment.GetEnvironmentVariable("AIRPORT") ?? "KSFO";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Worker starting - Topic: {Topic}, Airport: {Airport}", _topic, _airport);
        
        _consumer.Subscribe(_topic);

        // Start background task to periodically update repository
        var updateTask = Task.Run(() => PeriodicRepositoryUpdate(stoppingToken), stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult?.Message?.Value != null)
                    {
                        ProcessMessage(consumeResult.Message);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
            }
        }
        finally
        {
            _consumer.Close();
            await updateTask;
            _logger.LogInformation("Kafka Consumer Worker stopped");
        }
    }

    private void ProcessMessage(Message<string, string> message)
    {
        try
        {
            var positionEvent = JsonSerializer.Deserialize<PositionEvent>(message.Value);
            if (positionEvent != null)
            {
                // Convert to PositionDto and store in memory
                var positionDto = new PositionDto(
                    Id: positionEvent.Flight,
                    Icao: positionEvent.Icao24,
                    Lat: positionEvent.Lat,
                    Lon: positionEvent.Lon,
                    AltFt: positionEvent.AltFt,
                    GsKt: positionEvent.GsKt,
                    HeadingDeg: positionEvent.HdgDeg,
                    TsUtc: positionEvent.TsUtc,
                    WakeCat: DetermineWakeCategory(positionEvent.AltFt, positionEvent.GsKt)
                );

                _activePositions.AddOrUpdate(positionEvent.Icao24, positionDto, (key, old) => positionDto);
                
                _logger.LogDebug("Processed position for aircraft {Icao24} ({Flight})", 
                    positionEvent.Icao24, positionEvent.Flight);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message: {Message}", message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for aircraft {Key}", message.Key);
        }
    }

    private async Task PeriodicRepositoryUpdate(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Update repository every 5 seconds with current positions
                await Task.Delay(5000, stoppingToken);

                if (_activePositions.Any())
                {
                    // Remove old positions (older than 2 minutes)
                    var cutoff = DateTime.UtcNow.AddMinutes(-2);
                    var activePositions = _activePositions.Values
                        .Where(p => p.TsUtc > cutoff)
                        .ToList();

                    // Remove expired positions from dictionary
                    var keysToRemove = _activePositions
                        .Where(kvp => kvp.Value.TsUtc <= cutoff)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _activePositions.TryRemove(key, out _);
                    }

                    if (activePositions.Any())
                    {
                        await _repository.UpsertPositionsAsync(_airport, activePositions, stoppingToken);
                        _logger.LogDebug("Updated repository with {Count} active positions", activePositions.Count);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating repository");
            }
        }
    }

    private static string DetermineWakeCategory(int altFt, int gsKt)
    {
        // Simple wake category determination based on performance characteristics
        if (altFt > 35000 && gsKt > 450) return "H"; // Heavy
        if (altFt > 25000 && gsKt > 350) return "M"; // Medium
        return "L"; // Light
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}