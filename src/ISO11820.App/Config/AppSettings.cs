using System.Text.Json;

namespace ISO11820.App.Config;

public sealed class AppSettings
{
    public DatabaseSettings Database { get; init; } = new();

    public SimulationSettings Simulation { get; init; } = new();

    public OutputSettings Output { get; init; } = new();
}

public sealed class DatabaseSettings
{
    public string SqlitePath { get; init; } = "Data/ISO11820.db";
}

public sealed class SimulationSettings
{
    public bool EnableSimulation { get; init; } = true;

    public double StartTemperature { get; init; } = 720.0;

    public double HeatingRatePerSecond { get; init; } = 40.0;

    public double TargetTemperature { get; init; } = 750.0;

    public double StableThreshold { get; init; } = 3.0;

    public double TempFluctuation { get; init; } = 0.5;
}

public sealed class OutputSettings
{
    public string BaseDirectory { get; init; } = "TestData";
}

internal static class AppSettingsLoader
{
    public static AppSettings LoadDefault()
    {
        var path = Path.Combine(System.AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();
    }
}
