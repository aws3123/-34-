using System.Timers;
using ISO11820.App.Runtime.Controller;

namespace ISO11820.App.Runtime.Services;

public sealed class DaqWorker : IDisposable
{
    private readonly TestController _testController;
    private readonly System.Timers.Timer _timer;

    private const int TickIntervalMs = 800;

    public DaqWorker(TestController testController)
    {
        _testController = testController;
        _timer = new System.Timers.Timer(TickIntervalMs);
        _timer.AutoReset = true;
        _timer.Elapsed += OnTick;
    }

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;

        _testController.BroadcastInitialState();
        _timer.Start();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _timer.Stop();
        IsRunning = false;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        _testController.Tick();
    }
}
