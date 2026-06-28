using ISO11820.App.Runtime.Services;
using ISO11820.App.Shared.Events;
using ISO11820.App.Shared.Models;
using ISO11820.Core.Enums;
using ISO11820.Core.Models;

namespace ISO11820.App.Runtime.Controller;

public sealed class TestController
{
    private readonly SensorSimulator _sensorSimulator;
    private readonly List<SystemMessage> _pendingMessages = new();
    private readonly object _lock = new();

    private bool _isHeating;

    public TestController(SensorSimulator sensorSimulator)
    {
        _sensorSimulator = sensorSimulator;
        CurrentState = TestState.Idle;
        CurrentSnapshot = BuildSnapshot();
    }

    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    public TestState CurrentState { get; private set; }

    public RuntimeSnapshot CurrentSnapshot { get; private set; }

    public bool CanStartHeating => CurrentState == TestState.Idle;

    public bool CanStopHeating => CurrentState is TestState.Preparing or TestState.Ready;

    public bool CanStartRecording => CurrentState == TestState.Ready;

    public bool CanStopRecording => CurrentState == TestState.Recording;

    // --- User actions (called from UI thread) ---

    public void StartHeating()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Idle)
                return;

            _isHeating = true;
            _sensorSimulator.ResetStableCounter();
            TransitionTo(TestState.Preparing, "开始升温，系统升温中");
            Broadcast();
        }
    }

    public void StopHeating()
    {
        lock (_lock)
        {
            if (CurrentState is not TestState.Preparing and not TestState.Ready)
                return;

            _isHeating = false;
            _sensorSimulator.BeginCooling();
            TransitionTo(TestState.Idle, "停止加热，系统冷却中");
            Broadcast();
        }
    }

    public void StartRecording()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Ready)
                return;

            _sensorSimulator.ResetElapsed();
            TransitionTo(TestState.Recording, "开始记录，计时开始");
            Broadcast();
        }
    }

    public void StopRecording()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Recording)
                return;

            _sensorSimulator.ResetStableCounter();
            TransitionTo(TestState.Complete, "用户手动停止记录");
            Broadcast();
        }
    }

    public void CompleteTest(string? reason = null)
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Recording)
                return;

            _sensorSimulator.ResetStableCounter();
            string msg = reason ?? "记录时间到达 3600 秒，试验自动结束";
            TransitionTo(TestState.Complete, msg);
            Broadcast();
        }
    }

    public void ResetToIdle()
    {
        lock (_lock)
        {
            _isHeating = false;
            _sensorSimulator.ResetElapsed();
            _sensorSimulator.ResetStableCounter();
            TransitionTo(TestState.Idle, "系统已复位");
            Broadcast();
        }
    }

    // --- Tick driven by DaqWorker every 800ms (called from timer thread) ---

    public void Tick()
    {
        lock (_lock)
        {
            _pendingMessages.Clear();

            if (CurrentState == TestState.Idle && !_isHeating)
            {
                _sensorSimulator.UpdateCooling();
                Broadcast();
                return;
            }

            if (CurrentState == TestState.Idle)
            {
                Broadcast();
                return;
            }

            _sensorSimulator.Update(CurrentState);
            EvaluateAutoTransitions();
            Broadcast();
        }
    }

    public void BroadcastInitialState()
    {
        lock (_lock)
        {
            _pendingMessages.Clear();
            Broadcast();
        }
    }

    // --- Private helpers ---

    private void EvaluateAutoTransitions()
    {
        if (CurrentState == TestState.Preparing)
        {
            bool stable = _sensorSimulator.IsTemperatureStable();
            if (stable)
            {
                TransitionTo(TestState.Ready, "温度已稳定，可以开始记录");
            }
        }
        else if (CurrentState == TestState.Ready)
        {
            bool stable = _sensorSimulator.IsTemperatureStable();
            if (!stable)
            {
                _sensorSimulator.ResetStableCounter();
                TransitionTo(TestState.Preparing, "温度波动超出稳定范围，重新升温");
            }
        }
    }

    private void TransitionTo(TestState newState, string message)
    {
        CurrentState = newState;
        string time = DateTime.Now.ToString("HH:mm:ss");
        _pendingMessages.Add(new SystemMessage(time, message));
    }

    private void Broadcast()
    {
        CurrentSnapshot = BuildSnapshot();
        DataBroadcast?.Invoke(this, new DataBroadcastEventArgs(CurrentSnapshot));
    }

    private RuntimeSnapshot BuildSnapshot()
    {
        var temps = _sensorSimulator.CreateInitialSnapshot();
        return new RuntimeSnapshot(
            CurrentState,
            temps,
            _pendingMessages.ToArray(),
            _sensorSimulator.ElapsedSeconds);
    }
}
