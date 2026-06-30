namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class ProductMaster
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string? TestId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Specification { get; set; }

    public double? HeightMm { get; set; }

    public double? DiameterMm { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}
