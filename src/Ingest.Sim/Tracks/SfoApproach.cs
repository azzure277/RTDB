using Shared.Models;

namespace Ingest.Sim.Tracks;

public static class SfoApproach
{
    // Simple straight-in track generator ~12 NM final to 28L
    public static IEnumerable<PositionEvent> Generate(
        string icao24, string flight, DateTime start, int rateHz = 5)
    {
        // Threshold approx (KSFO 28L): 37.6189, -122.375
        double lat = 37.70, lon = -122.45; // start NW of threshold
        int alt = 3000; // feet
        int gs = 150;   // knots
        int vs = -700;  // fpm
        int hdg = 110;  // toward 28L (~284 inbound reciprocal)
        int fuel = 4000; // kg

        double dtSec = 1.0 / rateHz;
        var ts = start;

        for (int i = 0; i < rateHz * 180; i++) // ~3 minutes
        {
            yield return new PositionEvent(
                icao24, flight, lat, lon, alt, gs, hdg, vs, fuel, ts);

            // very rough forward integration: move east-southeast
            double nmPerSec = gs / 3600.0;
            double dLon = (nmPerSec * dtSec) / (60 * Math.Cos(lat * Math.PI/180));
            double dLat = -(nmPerSec * dtSec) / 60; // moving slightly south
            lon += dLon; lat += dLat; alt += (int)(vs * dtSec / 60.0);
            if (alt < 0) alt = 0;
            ts = ts.AddSeconds(dtSec);
            fuel -= 1; if (fuel < 0) fuel = 0;
        }
    }
}
