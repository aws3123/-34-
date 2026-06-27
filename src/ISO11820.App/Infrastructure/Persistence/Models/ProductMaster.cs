namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class ProductMaster
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}