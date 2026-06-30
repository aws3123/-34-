using ISO11820.App.Config;
using ISO11820.App.Infrastructure.FileStorage;
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
    private readonly Queue<double> _pidOutputQueue = new();
    private const int MaxPidSamples = 600;
    private readonly List<SensorDataRecord> _sensorDataBuffer = new();

    private bool _isHeating;
    private double _constantPower;

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

    /// <summary>
    /// The constant power value computed as the average PID output
    /// during the Ready state (up to 600 samples, ~8 minutes at 800ms tick).
    /// </summary>
    public double ConstantPower => _constantPower;

    public IReadOnlyList<SensorDataRecord> SensorDataBuffer
    {
        get { lock (_lock) { return _sensorDataBuffer.ToArray(); } }
    }

    // --- User actions (called from UI thread) ---

    public void StartHeating()
    {
        bool changed;
        lock (_lock)
        {
            if (CurrentState != TestState.Idle) { changed = false; }
            else
            {
                _isHeating = true;
                _sensorSimulator.ResetStableCounter();
                TransitionTo(TestState.Preparing, "开始升温，系统升温中");
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    public void StopHeating()
    {
        bool changed;
        lock (_lock)
        {
            if (CurrentState is not TestState.Preparing and not TestState.Ready) { changed = false; }
            else
            {
                _isHeating = false;
                _sensorSimulator.BeginCooling();
                TransitionTo(TestState.Idle, "停止加热，系统冷却中");
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    public void StartRecording()
    {
        bool changed;
        lock (_lock)
        {
            if (CurrentState != TestState.Ready) { changed = false; }
            else
            {
                if (_pidOutputQueue.Count > 0)
                {
                    _constantPower = _pidOutputQueue.Average();
                    _pidOutputQueue.Clear();
                }
                _sensorSimulator.ResetRecordingTimer();
                TransitionTo(TestState.Recording, "开始记录，计时开始");
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    public void StopRecording()
    {
        bool changed;
        lock (_lock)
        {
            if (CurrentState != TestState.Recording) { changed = false; }
            else
            {
                _sensorSimulator.ResetStableCounter();
                TransitionTo(TestState.Complete, "用户手动停止记录");
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    public void CompleteTest(string? reason = null)
    {
        bool changed;
        lock (_lock)
        {
            if (CurrentState != TestState.Recording) { changed = false; }
            else
            {
                _sensorSimulator.ResetStableCounter();
                string msg = reason ?? "记录时间到达 3600 秒，试验自动结束";
                TransitionTo(TestState.Complete, msg);
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    public void ResetToIdle()
    {
        lock (_lock)
        {
            _isHeating = false;
            _sensorSimulator.ResetElapsed();
            _sensorSimulator.ResetStableCounter();
            _sensorDataBuffer.Clear();
            TransitionTo(TestState.Idle, "系统已复位");
        }
        Broadcast();
    }

    public void UpdateSimulationSettings(SimulationSettings newSettings)
    {
        lock (_lock)
        {
            _sensorSimulator.UpdateSettings(newSettings);
            string time = DateTime.Now.ToString("HH:mm:ss");
            _pendingMessages.Add(new SystemMessage(time, "仿真参数已更新"));
        }
        Broadcast();
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
            }
            else if (CurrentState == TestState.Idle)
            {
                // no-op, just broadcast
            }
            else
            {
                _sensorSimulator.Update(CurrentState);

                AccumulateSensorData();

                if (CurrentState == TestState.Ready)
                {
                    var pidValue = _sensorSimulator.GetPidOutput();
                    _pidOutputQueue.Enqueue(pidValue);
                    if (_pidOutputQueue.Count > MaxPidSamples)
                        _pidOutputQueue.Dequeue();
                }

                EvaluateAutoTransitions();
                CheckAutoTermination();
            }
        }
        Broadcast();
    }

    public void BroadcastInitialState()
    {
        lock (_lock)
        {
            _pendingMessages.Clear();
        }
        Broadcast();
    }

    // --- Public queries ---

    /// <summary>
    /// 获取炉温1的温漂速率（°C/s），由 SensorSimulator 线性回归计算
    /// </summary>
    public double GetTemperatureDrift()
    {
        lock (_lock)
        {
            return _sensorSimulator.ComputeTemperatureDrift();
        }
    }

    // --- Private helpers ---

    private void AccumulateSensorData()
    {
        var temps = _sensorSimulator.CreateInitialSnapshot();
        var channelValues = new double[12];
        channelValues[0] = temps.Furnace1;
        channelValues[1] = temps.Furnace2;
        channelValues[2] = temps.Surface;
        channelValues[3] = temps.Center;
        channelValues[4] = temps.Calibration;
        // channels 5-11 remain 0 (placeholder)

        _sensorDataBuffer.Add(new SensorDataRecord
        {
            Timestamp = DateTime.Now,
            ChannelValues = channelValues,
        });
    }

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

    /// <summary>
    /// 检查自动终止条件：
    /// - 30/35/40/45/50/55 分钟检查点：温漂 ≤ 0.5 °C/10min 时提前终止
    /// - 60 分钟：无条件终止
    /// </summary>
    private void CheckAutoTermination()
    {
        if (CurrentState != TestState.Recording) return;

        var elapsed = _sensorSimulator.ElapsedSeconds;

        // 60 minutes — unconditional termination
        if (elapsed >= 3600)
        {
            CompleteTest();
            return;
        }

        // Early termination check points: 30, 35, 40, 45, 50, 55 minutes
        var checkPoints = new[] { 1800, 2100, 2400, 2700, 3000, 3300 };
        foreach (var point in checkPoints)
        {
            // Check within the 1-second window of each checkpoint
            if (elapsed >= point && elapsed < point + 1)
            {
                var driftPerTenMin = Math.Abs(_sensorSimulator.ComputeTemperatureDrift() * 600);
                if (driftPerTenMin <= 0.5)
                {
                    CompleteTest("满足终止条件，试验结束");
                    return;
                }
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
            _sensorSimulator.ElapsedSeconds,
            _sensorSimulator.ChartElapsedSeconds);
    }
}
