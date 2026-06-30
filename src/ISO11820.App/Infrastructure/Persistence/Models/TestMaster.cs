namespace ISO11820.App.Infrastructure.Persistence.Models;

public sealed class TestMaster
{
    public string ProductId { get; set; } = string.Empty;

    public string TestId { get; set; } = string.Empty;

    public string TestDate { get; set; } = string.Empty;

    public string? Operator { get; set; }

    public string? SampleName { get; set; }

    public string? Specification { get; set; }

    public double? HeightMm { get; set; }

    public double? DiameterMm { get; set; }

    public double? PreWeight { get; set; }

    public double? PostWeight { get; set; }

    public double? LostWeightPer { get; set; }

    public double? DeltaTf { get; set; }

    public int? TotalTestTime { get; set; }

    public int? FlameTime { get; set; }

    public int? FlameDuration { get; set; }

    public int HasFlame { get; set; }

    public double? EnvTemp { get; set; }

    public double? EnvHumidity { get; set; }

    public string? Notes { get; set; }

    public string? Flag { get; set; } = "00000000";

    public string CreatedAt { get; set; } = string.Empty;
}
