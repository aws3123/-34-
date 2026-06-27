namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class CalibrationRecord
{
    public long Id { get; set; }

    public long SensorId { get; set; }

    public string CalibrationDate { get; set; } = string.Empty;

    public string? ResultJson { get; set; }

    public string? Technician { get; set; }

    public string? Notes { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}