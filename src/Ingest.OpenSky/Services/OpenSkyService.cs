using Ingest.OpenSky.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ingest.OpenSky.Services;

public class OpenSkyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenSkyService> _logger;
    private readonly string _baseUrl = "https://opensky-network.org/api/states/all";

    public OpenSkyService(HttpClient httpClient, ILogger<OpenSkyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Set user agent as requested by OpenSky API
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ATC-System/1.0");
    }

    public async Task<IEnumerable<OpenSkyState>> GetStatesAsync(
        double? latMin = null, double? lonMin = null, 
        double? latMax = null, double? lonMax = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = _baseUrl;
            var queryParams = new List<string>();

            if (latMin.HasValue) queryParams.Add($"lamin={latMin:F6}");
            if (lonMin.HasValue) queryParams.Add($"lomin={lonMin:F6}");
            if (latMax.HasValue) queryParams.Add($"lamax={latMax:F6}");
            if (lonMax.HasValue) queryParams.Add($"lomax={lonMax:F6}");

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            _logger.LogDebug("Fetching OpenSky data from: {Url}", url);

            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var openSkyResponse = JsonSerializer.Deserialize<OpenSkyResponse>(response);

            if (openSkyResponse?.States == null)
            {
                _logger.LogWarning("No states returned from OpenSky API");
                return Enumerable.Empty<OpenSkyState>();
            }

            var states = openSkyResponse.States
                .Select(OpenSkyState.FromArray)
                .Where(s => s != null && 
                           !string.IsNullOrEmpty(s.Icao24) && 
                           s.Latitude.HasValue && 
                           s.Longitude.HasValue &&
                           !s.OnGround.GetValueOrDefault())
                .Cast<OpenSkyState>()
                .ToList();

            _logger.LogInformation("Retrieved {Count} airborne aircraft from OpenSky", states.Count);
            return states;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching OpenSky data");
            return Enumerable.Empty<OpenSkyState>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for OpenSky data");
            return Enumerable.Empty<OpenSkyState>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching OpenSky data");
            return Enumerable.Empty<OpenSkyState>();
        }
    }

    // Get aircraft in a specific region (e.g., around SFO)
    public async Task<IEnumerable<OpenSkyState>> GetSfoAreaAircraftAsync(CancellationToken cancellationToken = default)
    {
        // SFO area bounding box (roughly 100nm radius)
        const double sfoLat = 37.6213;
        const double sfoLon = -122.3790;
        const double radiusDeg = 1.5; // Approximately 100nm

        return await GetStatesAsync(
            latMin: sfoLat - radiusDeg,
            lonMin: sfoLon - radiusDeg,
            latMax: sfoLat + radiusDeg,
            lonMax: sfoLon + radiusDeg,
            cancellationToken);
    }

    // Get aircraft in LAX area
    public async Task<IEnumerable<OpenSkyState>> GetLaxAreaAircraftAsync(CancellationToken cancellationToken = default)
    {
        // LAX area bounding box
        const double laxLat = 33.9425;
        const double laxLon = -118.4081;
        const double radiusDeg = 1.5;

        return await GetStatesAsync(
            latMin: laxLat - radiusDeg,
            lonMin: laxLon - radiusDeg,
            latMax: laxLat + radiusDeg,
            lonMax: laxLon + radiusDeg,
            cancellationToken);
    }
}