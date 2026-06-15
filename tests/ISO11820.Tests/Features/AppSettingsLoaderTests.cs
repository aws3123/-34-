using ISO11820.App.Config;

namespace ISO11820.Tests.Features;

public sealed class AppSettingsLoaderTests
{
    [Fact]
    public void DefaultSettings_Should_Enable_Simulation_By_Default()
    {
        var settings = new AppSettings();

        Assert.True(settings.Simulation.EnableSimulation);
    }

    [Fact]
    public void ResolvePaths_Should_Combine_App_BaseDirectory_For_Relative_Paths()
    {
        var settings = new AppSettings
        {
            Database = new DatabaseSettings
            {
                SqlitePath = "Data/ISO11820.db"
            },
            Output = new OutputSettings
            {
                BaseDirectory = "TestData"
            }
        };

        var resolved = settings.ResolvePaths(@"C:\app\bin\Debug\net8.0-windows");

        Assert.Equal(Path.GetFullPath(@"C:\app\bin\Debug\net8.0-windows\Data\ISO11820.db"), resolved.Database.SqlitePath);
        Assert.Equal(Path.GetFullPath(@"C:\app\bin\Debug\net8.0-windows\TestData"), resolved.Output.BaseDirectory);
    }

    [Fact]
    public void ResolvePaths_Should_Keep_Absolute_Paths()
    {
        var settings = new AppSettings
        {
            Database = new DatabaseSettings
            {
                SqlitePath = @"D:\data\ISO11820.db"
            },
            Output = new OutputSettings
            {
                BaseDirectory = @"D:\exports"
            }
        };

        var resolved = settings.ResolvePaths(@"C:\app\bin\Debug\net8.0-windows");

        Assert.Equal(Path.GetFullPath(@"D:\data\ISO11820.db"), resolved.Database.SqlitePath);
        Assert.Equal(Path.GetFullPath(@"D:\exports"), resolved.Output.BaseDirectory);
    }
}
