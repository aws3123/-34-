namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class Apparatus
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string? SerialNumber { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}