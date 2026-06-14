using ISO11820.App.Config;
using ISO11820.App.Runtime.Controller;
using ISO11820.App.Runtime.Services;
using ISO11820.Core.Enums;

namespace ISO11820.Tests.Runtime;

public sealed class TestControllerTests
{
    private static (TestController, SensorSimulator) CreateController(
        double startTemp = 720.0,
        double targetTemp = 750.0,
        double heatingRate = 40.0,
        double tempFluctuation = 0.0)
    {
        var settings = new SimulationSettings
        {
            StartTemperature = startTemp,
            TargetTemperature = targetTemp,
            HeatingRatePerSecond = heatingRate,
            StableThreshold = 3.0,
            TempFluctuation = tempFluctuation
        };
        var sim = new SensorSimulator(settings);
        var controller = new TestController(sim);
        return (controller, sim);
    }

    [Fact]
    public void Initial_State_Is_Idle()
    {
        var (controller, _) = CreateController();

        Assert.Equal(TestState.Idle, controller.CurrentState);
    }

    [Fact]
    public void StartHeating_Transitions_Idle_To_Preparing()
    {
        var (controller, _) = CreateController();

        controller.StartHeating();

        Assert.Equal(TestState.Preparing, controller.CurrentState);
        Assert.False(controller.CanStartHeating);
    }

    [Fact]
    public void StartHeating_From_NonIdle_Is_Ignored()
    {
        var (controller, _) = CreateController();

        controller.StartHeating();
        // Calling again should not change state or cause issues
        controller.StartHeating();

        Assert.Equal(TestState.Preparing, controller.CurrentState);
    }

    [Fact]
    public void StartRecording_From_NonReady_Is_Ignored()
    {
        var (controller, _) = CreateController();

        controller.StartRecording();

        Assert.Equal(TestState.Idle, controller.CurrentState);
    }

    [Fact]
    public void StopHeating_Transitions_To_Idle()
    {
        var (controller, _) = CreateController();

        controller.StartHeating();
        Assert.Equal(TestState.Preparing, controller.CurrentState);

        controller.StopHeating();
        Assert.Equal(TestState.Idle, controller.CurrentState);
    }

    [Fact]
    public void Tick_Preparing_To_Ready_When_Temperature_Stable()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        controller.StartHeating();
        Assert.Equal(TestState.Preparing, controller.CurrentState);

        // Run enough ticks for stable counter to exceed 3
        for (int i = 0; i < 10; i++)
        {
            controller.Tick();
            if (controller.CurrentState == TestState.Ready) break;
        }

        Assert.Equal(TestState.Ready, controller.CurrentState);
    }

    [Fact]
    public void StartRecording_Transitions_Ready_To_Recording()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        controller.StartHeating();

        // Tick to reach Ready
        for (int i = 0; i < 10; i++)
        {
            controller.Tick();
            if (controller.CurrentState == TestState.Ready) break;
        }
        Assert.Equal(TestState.Ready, controller.CurrentState);

        controller.StartRecording();
        Assert.Equal(TestState.Recording, controller.CurrentState);
    }

    [Fact]
    public void StopRecording_Transitions_Recording_To_Complete()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        controller.StartHeating();
        for (int i = 0; i < 10; i++)
        {
            controller.Tick();
            if (controller.CurrentState == TestState.Ready) break;
        }
        controller.StartRecording();
        Assert.Equal(TestState.Recording, controller.CurrentState);

        controller.StopRecording();

        Assert.Equal(TestState.Complete, controller.CurrentState);
    }

    [Fact]
    public void CompleteTest_Transitions_Recording_To_Complete()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        controller.StartHeating();
        for (int i = 0; i < 10; i++) controller.Tick();
        controller.StartRecording();

        controller.CompleteTest();

        Assert.Equal(TestState.Complete, controller.CurrentState);
    }

    [Fact]
    public void ResetToIdle_Sets_Idle_From_Any_State()
    {
        var (controller, _) = CreateController();

        controller.StartHeating();
        Assert.NotEqual(TestState.Idle, controller.CurrentState);

        controller.ResetToIdle();

        Assert.Equal(TestState.Idle, controller.CurrentState);
    }

    [Fact]
    public void Broadcast_Contains_RuntimeSnapshot_With_Temperatures()
    {
        var (controller, _) = CreateController();
        RuntimeSnapshot? received = null;
        controller.DataBroadcast += (_, e) => received = e.Snapshot;

        controller.BroadcastInitialState();

        Assert.NotNull(received);
        Assert.NotNull(received!.Temperatures);
        Assert.Equal(TestState.Idle, received.State);
    }

    [Fact]
    public void Broadcast_Includes_Transition_Messages()
    {
        var (controller, _) = CreateController();
        List<RuntimeSnapshot> snapshots = new();
        controller.DataBroadcast += (_, e) => snapshots.Add(e.Snapshot);

        controller.StartHeating();

        var lastSnapshot = snapshots.Last();
        Assert.Contains(lastSnapshot.Messages, m => m.Message.Contains("升温"));
    }

    [Fact]
    public void CanStartHeating_Only_True_In_Idle()
    {
        var (controller, _) = CreateController();

        Assert.True(controller.CanStartHeating);

        controller.StartHeating();
        Assert.False(controller.CanStartHeating);
    }

    [Fact]
    public void CanStopHeating_True_In_Preparing_Or_Ready()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        Assert.False(controller.CanStopHeating);

        controller.StartHeating();
        Assert.True(controller.CanStopHeating);

        for (int i = 0; i < 10; i++)
        {
            controller.Tick();
            if (controller.CurrentState == TestState.Ready) break;
        }
        Assert.True(controller.CanStopHeating);

        controller.StartRecording();
        Assert.False(controller.CanStopHeating);
    }

    [Fact]
    public void CanStartRecording_Only_True_In_Ready()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        Assert.False(controller.CanStartRecording);

        controller.StartHeating();
        Assert.False(controller.CanStartRecording);

        for (int i = 0; i < 10; i++)
        {
            controller.Tick();
            if (controller.CurrentState == TestState.Ready) break;
        }
        Assert.True(controller.CanStartRecording);
    }

    [Fact]
    public void Snapshot_Includes_ElapsedSeconds()
    {
        var (controller, sim) = CreateController(
            startTemp: 749.0, targetTemp: 750.0, tempFluctuation: 0.0);

        controller.StartHeating();
        for (int i = 0; i < 10; i++) controller.Tick();
        controller.StartRecording();

        // Simulate a few recording ticks
        for (int i = 0; i < 5; i++) controller.Tick();

        Assert.True(controller.CurrentSnapshot.ElapsedSeconds > 0);
    }
}
