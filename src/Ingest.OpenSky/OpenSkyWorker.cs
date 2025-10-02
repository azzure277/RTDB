using Ingest.OpenSky.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Ingest.OpenSky;

public class OpenSkyWorker : BackgroundService
{
    private readonly OpenSkyService _openSkyService;
    private readonly OpenSkyKafkaPublisher _publisher;
    private readonly ILogger<OpenSkyWorker> _logger;
    private readonly int _intervalSeconds;
    private readonly string _region;

    public OpenSkyWorker(
        OpenSkyService openSkyService,
        OpenSkyKafkaPublisher publisher,
        ILogger<OpenSkyWorker> logger,
        IConfiguration configuration)
    {
        _openSkyService = openSkyService;
        _publisher = publisher;
        _logger = logger;
        _intervalSeconds = configuration.GetValue<int>("INTERVAL_SECONDS", 30);
        _region = configuration.GetValue<string>("REGION", "SFO") ?? "SFO";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OpenSky Worker starting - Region: {Region}, Interval: {Interval}s", 
            _region, _intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                var aircraft = _region.ToUpper() switch
                {
                    "LAX" => await _openSkyService.GetLaxAreaAircraftAsync(stoppingToken),
                    "SFO" or _ => await _openSkyService.GetSfoAreaAircraftAsync(stoppingToken)
                };

                if (aircraft.Any())
                {
                    await _publisher.PublishBatchAsync(aircraft, stoppingToken);
                    
                    var processingTime = DateTime.UtcNow - startTime;
                    _logger.LogInformation("Processed {Count} aircraft in {Duration}ms", 
                        aircraft.Count(), processingTime.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("No aircraft data received from OpenSky API");
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OpenSky Worker cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenSky Worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("OpenSky Worker stopped");
    }
}