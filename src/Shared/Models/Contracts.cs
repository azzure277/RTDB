namespace Shared.Models;

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
