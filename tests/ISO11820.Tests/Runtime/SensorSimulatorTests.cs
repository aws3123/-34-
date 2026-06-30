using ISO11820.App.Config;
using ISO11820.App.Runtime.Services;
using ISO11820.Core.Enums;

namespace ISO11820.Tests.Runtime;

public sealed class SensorSimulatorTests
{
    private static SimulationSettings CreateSettings(
        double startTemp = 25.0,
        double heatingRate = 40.0,
        double targetTemp = 750.0,
        double stableThreshold = 3.0,
        double tempFluctuation = 0.0)
    {
        return new SimulationSettings
        {
            StartTemperature = startTemp,
            HeatingRatePerSecond = heatingRate,
            TargetTemperature = targetTemp,
            StableThreshold = stableThreshold,
            TempFluctuation = tempFluctuation
        };
    }

    [Fact]
    public void CreateInitialSnapshot_Uses_StartTemperature()
    {
        var settings = CreateSettings(startTemp: 720.0);
        var sim = new SensorSimulator(settings);

        var snap = sim.CreateInitialSnapshot();

        Assert.Equal(720.0, snap.Furnace1);
        Assert.Equal(720.0, snap.Furnace2);
        Assert.Equal(720.0 * 0.3, snap.Surface);
        Assert.Equal(720.0 * 0.25, snap.Center);
        Assert.Equal(0, snap.ElapsedSeconds);
    }

    [Fact]
    public void Update_Preparing_Increases_FurnaceTemperature()
    {
        var settings = CreateSettings(heatingRate: 40.0, tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        var before = sim.CreateInitialSnapshot();
        var after = sim.Update(TestState.Preparing);

        // With 0 noise, TF1 should increase by exactly 40 * 0.8 = 32
        Assert.True(after.Furnace1 > before.Furnace1,
            $"TF1 should increase: {before.Furnace1} -> {after.Furnace1}");
        Assert.True(after.Furnace2 > before.Furnace2);
    }

    [Fact]
    public void Update_Preparing_Clamps_To_TargetTemperature()
    {
        var settings = CreateSettings(
            startTemp: 745.0,
            heatingRate: 40.0,
            targetTemp: 750.0,
            tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        var snap = sim.Update(TestState.Preparing);

        // At 745+ with threshold 3, it should clamp to target
        Assert.True(snap.Furnace1 <= 750.0,
            $"TF1 should be clamped to 750, got {snap.Furnace1}");
    }

    [Fact]
    public void IsTemperatureStable_ReturnsFalse_WhenTemperatureTooLow()
    {
        var settings = CreateSettings(startTemp: 25.0, targetTemp: 750.0);
        var sim = new SensorSimulator(settings);

        // Update once then check — temp should be far from stable
        sim.Update(TestState.Preparing);
        bool stable = sim.IsTemperatureStable();

        Assert.False(stable);
    }

    [Fact]
    public void IsTemperatureStable_Requires_MultipleConsecutiveStableTicks()
    {
        var settings = CreateSettings(
            startTemp: 749.0,
            targetTemp: 750.0,
            stableThreshold: 3.0,
            tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        bool stable = false;
        for (int i = 0; i < 5; i++)
        {
            sim.Update(TestState.Preparing);
            stable = sim.IsTemperatureStable();
        }

        Assert.True(stable);
    }

    [Fact]
    public void ElapsedSeconds_Increments_By_Whole_Seconds_From_Recording_Ticks()
    {
        var settings = CreateSettings();
        var sim = new SensorSimulator(settings);

        sim.Update(TestState.Preparing);
        Assert.Equal(0, sim.ElapsedSeconds);

        sim.Update(TestState.Recording);
        Assert.Equal(0, sim.ElapsedSeconds);

        sim.Update(TestState.Recording);
        Assert.Equal(1, sim.ElapsedSeconds);

        sim.Update(TestState.Recording);
        Assert.Equal(2, sim.ElapsedSeconds);

        sim.Update(TestState.Complete);
        Assert.Equal(2, sim.ElapsedSeconds);
    }

    [Fact]
    public void Update_Recording_Raises_SurfaceAndCenterTemperatures()
    {
        var settings = CreateSettings(
            startTemp: 750.0,
            targetTemp: 750.0,
            tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        var first = sim.Update(TestState.Recording);

        // After many recording ticks, TS and TC should be approaching their targets
        for (int i = 0; i < 100; i++)
            sim.Update(TestState.Recording);

        var later = sim.Update(TestState.Recording);

        Assert.True(later.Surface > first.Surface,
            $"Surface should rise during recording: {first.Surface} -> {later.Surface}");
        Assert.True(later.Center > first.Center,
            $"Center should rise during recording: {first.Center} -> {later.Center}");
    }

    [Fact]
    public void Update_Idle_DoesNotChangeTemperatures_Significantly()
    {
        var settings = CreateSettings(tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        var before = sim.CreateInitialSnapshot();
        var after = sim.Update(TestState.Idle);

        Assert.Equal(before.Furnace1, after.Furnace1);
        Assert.Equal(before.Furnace2, after.Furnace2);
    }

    [Fact]
    public void ResetElapsed_Resets_ElapsedSeconds_ToZero()
    {
        var settings = CreateSettings();
        var sim = new SensorSimulator(settings);

        sim.Update(TestState.Recording);
        sim.Update(TestState.Recording);
        sim.Update(TestState.Recording);
        Assert.Equal(2, sim.ElapsedSeconds);

        sim.ResetElapsed();
        Assert.Equal(0, sim.ElapsedSeconds);
    }

    [Fact]
    public void UpdateCooling_Decreases_FurnaceTemperature()
    {
        var settings = CreateSettings(startTemp: 750.0, tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        // Get a baseline
        sim.Update(TestState.Ready);

        sim.UpdateCooling();
        sim.UpdateCooling();

        var snap = sim.CreateInitialSnapshot();

        Assert.True(snap.Furnace1 < 750.0,
            $"Cooling should decrease temperature, got {snap.Furnace1}");
        Assert.True(snap.Furnace2 < 750.0);
    }

    [Fact]
    public void StableCounter_Resets_WhenTemperatureOutOfRange()
    {
        var settings = CreateSettings(
            startTemp: 749.0,
            targetTemp: 750.0,
            stableThreshold: 3.0,
            tempFluctuation: 0.0);
        var sim = new SensorSimulator(settings);

        bool stable = false;
        for (int i = 0; i < 5; i++)
        {
            sim.Update(TestState.Preparing);
            stable = sim.IsTemperatureStable();
        }

        Assert.True(stable);

        sim.ResetStableCounter();
        Assert.False(sim.IsTemperatureStable());
    }
}
