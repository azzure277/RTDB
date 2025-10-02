namespace Contracts;

public sealed record PositionDto(
    string Id,           // callsign/hex
    string Icao,         // airport context (e.g., KSFO) if relevant
    double Lat,
    double Lon,
    int AltFt,
    int GsKt,
    int HeadingDeg,
    DateTime TsUtc,
    string? WakeCat = null // L/M/H/J if known
);

public sealed record StateDto(
    string Airport,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<string> Sequence,
    DateTime TsUtc
);
