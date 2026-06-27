namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class TestMaster
{
    public long Id { get; set; }

    public string TestType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Standard { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}