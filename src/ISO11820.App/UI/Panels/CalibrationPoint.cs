namespace ISO11820.App.UI.Panels;

public sealed class CalibrationPoint
{
    public double Ref { get; set; }
    public double Measured { get; set; }
    public double Deviation { get; set; }
    public string Time { get; set; } = string.Empty;
}