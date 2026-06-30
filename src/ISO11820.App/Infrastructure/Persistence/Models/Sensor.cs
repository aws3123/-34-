namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class Sensor
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int? Channel { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}