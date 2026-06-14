using ISO11820.App.Runtime.Services;
using ISO11820.App.Shared.Events;
using ISO11820.App.Shared.Models;
using ISO11820.Core.Enums;
using ISO11820.Core.Models;

namespace ISO11820.App.Runtime.Controller;

public sealed class TestController
{
    private readonly SensorSimulator _sensorSimulator;

    public TestController(SensorSimulator sensorSimulator)
    {
        _sensorSimulator = sensorSimulator;
        CurrentState = TestState.Idle;
        CurrentSnapshot = new RuntimeSnapshot(CurrentState, _sensorSimulator.CreateInitialSnapshot(), Array.Empty<SystemMessage>());
    }

    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    public TestState CurrentState { get; private set; }

    public RuntimeSnapshot CurrentSnapshot { get; private set; }

    public void BroadcastInitialState()
    {
        DataBroadcast?.Invoke(this, new DataBroadcastEventArgs(CurrentSnapshot));
    }
}
