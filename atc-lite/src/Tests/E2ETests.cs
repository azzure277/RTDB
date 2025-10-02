using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class E2ETests
{
    private const string ApiBase = "http://localhost:5000"; // Adjust if needed
    private static readonly string Jwt = Environment.GetEnvironmentVariable("E2E_JWT") ?? "<INSERT_DEV_JWT_HERE>";

    [Fact]
    public async Task IngestAndStateFlow_WorksWithJwt()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Jwt);

        // 1. POST /api/ingest
        var ingestPositions = new[] {
            new {
                Id = "A1",
                Icao = "KSFO",
                Lat = 37.62,
                Lon = -122.38,
                AltFt = 2000,
                GsKt = 150,
                HeadingDeg = 110,
                TsUtc = DateTime.UtcNow,
                WakeCat = "M"
            }
        };
        var content = JsonContent.Create(ingestPositions);
        var ingestResp = await client.PostAsync($"{ApiBase}/api/ingest?airport=KSFO", content);
        Assert.True(ingestResp.IsSuccessStatusCode, $"Ingest failed: {ingestResp.StatusCode}");

        // 2. Poll /api/state
        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            var stateResp = await client.GetAsync($"{ApiBase}/api/state?airport=KSFO");
            if (!stateResp.IsSuccessStatusCode) continue;
            var json = await stateResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("positions", out var positions) && positions.GetArrayLength() > 0)
            {
                found = true;
                break;
            }
            await Task.Delay(500);
        }
        Assert.True(found, "Did not find ingested position in state");
    }
}
