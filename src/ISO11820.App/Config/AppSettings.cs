using System.Text.Json;

namespace ISO11820.App.Config;

public sealed class AppSettings
{
    public DatabaseSettings Database { get; init; } = new();

    public SimulationSettings Simulation { get; init; } = new();

    public OutputSettings Output { get; init; } = new();

    public AppSettings ResolvePaths(string baseDirectory)
    {
        return new AppSettings
        {
            Database = Database.ResolvePaths(baseDirectory),
            Simulation = new SimulationSettings
            {
                EnableSimulation = Simulation.EnableSimulation,
                StartTemperature = Simulation.StartTemperature,
                HeatingRatePerSecond = Simulation.HeatingRatePerSecond,
                TargetTemperature = Simulation.TargetTemperature,
                StableThreshold = Simulation.StableThreshold
            },
            Output = Output.ResolvePaths(baseDirectory)
        };
    }
}

public sealed class DatabaseSettings
{
    public string SqlitePath { get; init; } = "Data/ISO11820.db";

    public DatabaseSettings ResolvePaths(string baseDirectory)
    {
        var sqlitePath = string.IsNullOrWhiteSpace(SqlitePath)
            ? "Data/ISO11820.db"
            : SqlitePath;

        return new DatabaseSettings
        {
            SqlitePath = AppSettingsPathResolver.Resolve(baseDirectory, sqlitePath)
        };
    }
}

public sealed class SimulationSettings
{
    public bool EnableSimulation { get; init; } = true;

    public double StartTemperature { get; init; } = 720.0;

    public double HeatingRatePerSecond { get; init; } = 40.0;

    public double TargetTemperature { get; init; } = 750.0;

    public double StableThreshold { get; init; } = 3.0;
}

public sealed class OutputSettings
{
    public string BaseDirectory { get; init; } = "TestData";

    public OutputSettings ResolvePaths(string baseDirectory)
    {
        var outputDirectory = string.IsNullOrWhiteSpace(BaseDirectory)
            ? "TestData"
            : BaseDirectory;

        return new OutputSettings
        {
            BaseDirectory = AppSettingsPathResolver.Resolve(baseDirectory, outputDirectory)
        };
    }
}

internal static class AppSettingsLoader
{
    public static AppSettings LoadDefault()
    {
        var baseDirectory = System.AppContext.BaseDirectory;
        var path = Path.Combine(baseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new AppSettings().ResolvePaths(baseDirectory);
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();

        return settings.ResolvePaths(baseDirectory);
    }
}

internal static class AppSettingsPathResolver
{
    public static string Resolve(string baseDirectory, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}
