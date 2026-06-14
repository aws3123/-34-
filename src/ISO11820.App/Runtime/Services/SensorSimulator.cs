using ISO11820.App.Config;
using ISO11820.Core.Models;

namespace ISO11820.App.Runtime.Services;

public sealed class SensorSimulator
{
    private readonly SimulationSettings _settings;

    public SensorSimulator(SimulationSettings settings)
    {
        _settings = settings;
    }

    public TemperatureSnapshot CreateInitialSnapshot()
    {
        var start = _settings.StartTemperature;
        return new TemperatureSnapshot(start, start, start * 0.3, start * 0.25, start, 0);
    }
}
