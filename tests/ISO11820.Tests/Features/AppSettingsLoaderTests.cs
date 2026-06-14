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
}
