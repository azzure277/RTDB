namespace Shared.Models;

public enum WakeClass
{
    Light,
    Medium,
    Heavy
}


public static class WakeMinima
{
    // Leader x Follower (NM)
    private static readonly Dictionary<(WakeClass, WakeClass), int> _nm = new()
    {
        { (WakeClass.Light,  WakeClass.Light),  3 },
        { (WakeClass.Light,  WakeClass.Medium), 3 },
        { (WakeClass.Light,  WakeClass.Heavy),  4 },

        { (WakeClass.Medium, WakeClass.Light),  5 },
        { (WakeClass.Medium, WakeClass.Medium), 3 },
        { (WakeClass.Medium, WakeClass.Heavy),  4 },

        { (WakeClass.Heavy,  WakeClass.Light),  6 },
        { (WakeClass.Heavy,  WakeClass.Medium), 5 },
        { (WakeClass.Heavy,  WakeClass.Heavy),  4 },
    };

    public static int RequiredNm(WakeClass leader, WakeClass follower) =>
        _nm.TryGetValue((leader, follower), out var nm) ? nm : 3;
}

public record PositionEvent(
    string Icao24,
    string Flight,
    double Lat,
    double Lon,
    int AltFt,
    int GsKt,
    int HdgDeg,
    int VsFpm,
    int FuelKg,
    DateTime TsUtc
);

public record WeatherSnapshot(
    string Icao,
    int WindDirDeg,
    int WindKt,
    int? CeilingFt,
    int VisSm,
    string Metar,
    DateTime TsUtc
);

public record RunwayState(
    string Icao,
    string Runway,
    string Status,
    int MinSepNm,
    int TailwindLimitKt
);

public record Advisory(
    string Icao,
    string Type,
    object Data,
    DateTime TsUtc
);
