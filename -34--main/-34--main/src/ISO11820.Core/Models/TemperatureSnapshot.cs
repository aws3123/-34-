namespace ISO11820.Core.Models;

public sealed record TemperatureSnapshot(
    double Furnace1,
    double Furnace2,
    double Surface,
    double Center,
    double Calibration,
    int ElapsedSeconds);
