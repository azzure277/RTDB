namespace Contracts;

public sealed record PositionDto(
    string Id,           // callsign/hex
    string Icao,         // airport context (e.g., KSFO) if relevant
    string Flight,       // flight number/callsign
    string Icao24,       // hex code
    double Lat,
    double Lon,
    int AltFt,
    int GsKt,
    int HeadingDeg,
    int? DistNm,         // distance in NM (optional)
    int? FuelKg,         // fuel in kg (optional)
    DateTime TsUtc,
    string? WakeCat = null // L/M/H/J if known
);

public sealed record StateDto(
    string Airport,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<string> Sequence,
    DateTime TsUtc
);
