using ISO11820.App.Config;
using ISO11820.Core.Enums;
using ISO11820.Core.Models;

namespace ISO11820.App.Runtime.Services;

public sealed class SensorSimulator
{
    private readonly SimulationSettings _settings;
    private readonly Random _random = new();

    private double _tf1;
    private double _tf2;
    private double _ts;
    private double _tc;

    private int _stableTickCount;
    private int _elapsedSeconds;

    public SensorSimulator(SimulationSettings settings)
    {
        _settings = settings;
        _tf1 = settings.StartTemperature;
        _tf2 = settings.StartTemperature;
        _ts = settings.StartTemperature * 0.3;
        _tc = settings.StartTemperature * 0.25;
    }

    public int StableTickCount => _stableTickCount;
    public int ElapsedSeconds => _elapsedSeconds;

    public TemperatureSnapshot CreateInitialSnapshot()
    {
        return new TemperatureSnapshot(_tf1, _tf2, _ts, _tc, _tf1, _elapsedSeconds);
    }

    public TemperatureSnapshot Update(TestState state)
    {
        double noise = Noise();

        switch (state)
        {
            case TestState.Preparing:
                AdvancePreparing(noise);
                break;
            case TestState.Ready:
                AdvanceStable(noise);
                break;
            case TestState.Recording:
                AdvanceRecording(noise);
                _elapsedSeconds++;
                break;
            case TestState.Complete:
                AdvanceStable(noise);
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
            Math.Round(_tf1 + noise * 2, 1),
            _elapsedSeconds);
    }

    public void ResetElapsed()
    {
        _elapsedSeconds = 0;
    }

    public void ResetStableCounter()
    {
        _stableTickCount = 0;
    }

    public void BeginCooling()
    {
        // Reset stable counter when cooling starts
        _stableTickCount = 0;
    }

    public void UpdateCooling()
    {
        double noise = Noise();
        _tf1 = Math.Max(25.0, _tf1 - 0.5 + noise * 0.1);
        _tf2 = Math.Max(25.0, _tf2 - 0.5 + noise * 0.1);
        _ts = _tf1 * 0.3 + noise;
        _tc = _tf1 * 0.25 + noise;
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

    private void AdvancePreparing(double noise)
    {
        double step = _settings.HeatingRatePerSecond * 0.8;

        _tf1 = Math.Min(_settings.TargetTemperature, _tf1 + step + noise);
        _tf2 = Math.Min(_settings.TargetTemperature, _tf2 + step + noise * 0.9);

        // Clamp to target once reached
        if (_tf1 >= _settings.TargetTemperature - _settings.StableThreshold)
        {
            _tf1 = _settings.TargetTemperature + noise;
            _tf2 = _settings.TargetTemperature + noise * 0.9;
        }

        _ts = _tf1 * 0.3 + noise * 0.5;
        _tc = _tf1 * 0.25 + noise * 0.3;
    }

    private void AdvanceStable(double noise)
    {
        _tf1 = _settings.TargetTemperature + noise;
        _tf2 = _settings.TargetTemperature + noise * 0.9;

        _ts = _tf1 * 0.3 + noise * 0.5;
        _tc = _tf1 * 0.25 + noise * 0.3;
    }

    private void AdvanceRecording(double noise)
    {
        _tf1 = _settings.TargetTemperature + noise;
        _tf2 = _settings.TargetTemperature + noise * 0.9;

        double surfaceTarget = Math.Min(_tf1 * 0.95, 800);
        _ts += (surfaceTarget - _ts) * 0.02 + noise * 0.5;

        double centerTarget = Math.Min(_tf1 * 0.85, 750);
        _tc += (centerTarget - _tc) * 0.01 + noise * 0.3;
    }

    private double Noise()
    {
        return (_random.NextDouble() * 2 - 1) * _settings.TempFluctuation;
    }
}
