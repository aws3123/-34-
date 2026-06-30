using ISO11820.App.Config;
using ISO11820.Core.Enums;
using ISO11820.Core.Models;
using MathNet.Numerics;

namespace ISO11820.App.Runtime.Services;

public sealed class SensorSimulator
{
    private SimulationSettings _settings;
    private readonly Random _random = new();

    private double _tf1;
    private double _tf2;
    private double _ts;
    private double _tc;

    private const int RecordingTickMilliseconds = 800;

    private int _stableTickCount;
    private int _elapsedMilliseconds;
    private int _chartTickCount;

    // 温漂计算：记录最近 N 个采样点用于线性回归
    private readonly List<(double time, double temp)> _recentFurnace1Samples = new();
    private const int MaxDriftSamples = 20;

    public SensorSimulator(SimulationSettings settings)
    {
        _settings = settings;
        _tf1 = settings.StartTemperature;
        _tf2 = settings.StartTemperature;
        _ts = settings.StartTemperature * 0.3;
        _tc = settings.StartTemperature * 0.25;
    }

    public int StableTickCount => _stableTickCount;
    public int ElapsedSeconds => _elapsedMilliseconds / 1000;
    public int ChartElapsedSeconds => _chartTickCount * RecordingTickMilliseconds / 1000;

    public TemperatureSnapshot CreateInitialSnapshot()
    {
        return new TemperatureSnapshot(_tf1, _tf2, _ts, _tc, _tf1 + Noise() * 2, ElapsedSeconds);
    }

    public TemperatureSnapshot Update(TestState state)
    {
        switch (state)
        {
            case TestState.Preparing:
                AdvancePreparing();
                _chartTickCount++;
                break;
            case TestState.Ready:
                AdvanceStable();
                _chartTickCount++;
                break;
            case TestState.Recording:
                AdvanceRecording();
                _elapsedMilliseconds += RecordingTickMilliseconds;
                _chartTickCount++;
                TrackDriftSample();
                break;
            case TestState.Complete:
                AdvanceStable();
                break;
            default:
                // Idle: no active heating — optionally drift toward ambient
                break;
        }

        return new TemperatureSnapshot(
            Math.Round(_tf1, 1),
            Math.Round(_tf2, 1),
            Math.Round(_ts, 1),
            Math.Round(_tc, 1),
            Math.Round(_tf1 + Noise() * 2, 1),
            ElapsedSeconds);
    }

    /// <summary>
    /// 使用 MathNet.Numerics 线性回归计算炉温1的温漂（°C/s）
    /// </summary>
    public double ComputeTemperatureDrift()
    {
        lock (_recentFurnace1Samples)
        {
            if (_recentFurnace1Samples.Count < 3)
                return 0.0;

            var times = _recentFurnace1Samples.Select(s => s.time).ToArray();
            var temps = _recentFurnace1Samples.Select(s => s.temp).ToArray();

            var (intercept, slope) = Fit.Line(times, temps);
            return slope;
        }
    }

    private void TrackDriftSample()
    {
        lock (_recentFurnace1Samples)
        {
            _recentFurnace1Samples.Add((_elapsedMilliseconds / 1000.0, _tf1));
            if (_recentFurnace1Samples.Count > MaxDriftSamples)
                _recentFurnace1Samples.RemoveAt(0);
        }
    }

    public void UpdateSettings(SimulationSettings newSettings)
    {
        _settings = newSettings;
    }

    public void ResetElapsed()
    {
        _elapsedMilliseconds = 0;
        _chartTickCount = 0;
        lock (_recentFurnace1Samples)
        {
            _recentFurnace1Samples.Clear();
        }
    }

    public void ResetRecordingTimer()
    {
        _elapsedMilliseconds = 0;
    }

    public void ResetStableCounter()
    {
        _stableTickCount = 0;
    }

    public void BeginCooling()
    {
        _stableTickCount = 0;
    }

    public void UpdateCooling()
    {
        _tf1 = Math.Max(25.0, _tf1 - 0.5 + Noise() * 0.1);
        _tf2 = Math.Max(25.0, _tf2 - 0.5 + Noise() * 0.1);
        _ts = _tf1 * 0.3 + Noise();
        _tc = _tf1 * 0.25 + Noise();
    }

    public bool IsTemperatureStable()
    {
        bool inRange = _tf1 >= _settings.TargetTemperature - _settings.StableThreshold
                    && _tf1 <= _settings.TargetTemperature + _settings.StableThreshold;

        if (inRange)
            _stableTickCount++;
        else
            _stableTickCount = 0;

        return _stableTickCount > 3;
    }

    /// <summary>
    /// 升温阶段：TF1 &lt; TargetTemp - StableThreshold（&lt; 747°C）
    /// </summary>
    private void AdvancePreparing()
    {
        double step = _settings.HeatingRatePerSecond * 0.8;

        if (_tf1 < _settings.TargetTemperature - _settings.StableThreshold)
        {
            // 升温阶段：线性递增
            _tf1 += step + Noise();
            _tf2 += step + Noise();
        }
        else
        {
            // 稳定阶段（TF1 >= 747°C）：钳位到目标温度
            _tf1 = _settings.TargetTemperature + Noise();
            _tf2 = _settings.TargetTemperature + Noise();
        }

        _ts = _tf1 * 0.3 + Noise();
        _tc = _tf1 * 0.25 + Noise();
    }

    /// <summary>
    /// 稳定阶段（Ready）：TF1/TF2 钳位到目标温度 + 独立噪声
    /// </summary>
    private void AdvanceStable()
    {
        _tf1 = _settings.TargetTemperature + Noise();
        _tf2 = _settings.TargetTemperature + Noise();

        _ts = _tf1 * 0.3 + Noise();
        _tc = _tf1 * 0.25 + Noise();
    }

    /// <summary>
    /// 记录阶段（Recording）：表面温和中心温指数逼近炉温
    /// </summary>
    private void AdvanceRecording()
    {
        _tf1 = _settings.TargetTemperature + Noise();
        _tf2 = _settings.TargetTemperature + Noise();

        double surfaceTarget = Math.Min(_tf1 * 0.95, 800);
        _ts += (surfaceTarget - _ts) * 0.02 + Noise();

        double centerTarget = Math.Min(_tf1 * 0.85, 750);
        _tc += (centerTarget - _tc) * 0.01 + Noise();
    }

    /// <summary>
    /// Simulated PID output value.
    /// </summary>
    public double GetPidOutput()
    {
        return 2048.0 + Noise() * 10;
    }

    private double Noise()
    {
        return (_random.NextDouble() * 2 - 1) * _settings.TempFluctuation;
    }
}