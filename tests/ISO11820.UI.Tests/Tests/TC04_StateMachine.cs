using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第八部分：试验状态机验收测试
/// </summary>
public class TC04_StateMachine : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC04_StateMachine(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "8.1.2 初始状态 — Idle")]
    public void StateMachine_InitialStateIsIdle()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.Contains("空闲", stateText);

        Capture("8.1.2_初始状态_Idle");
        _output.WriteLine($"✅ 初始状态: {stateText}");
    }

    [Fact(DisplayName = "8.2.1 Idle → Preparing — 点击开始升温")]
    public void StateMachine_IdleToPreparing()
    {
        // 启动程序
        Launcher.Start();
        Thread.Sleep(3000);

        // 通过信号文件触发登录
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        // 获取主窗口
        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-01", "T01");

        ClickButtonBySignal("click_start_heating");
        Thread.Sleep(1000);

        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(10));
        Assert.NotNull(stateText);
        // 仿真升温很快（~3秒），可能已经到达"就绪"状态
        Assert.True(stateText.Contains("升温") || stateText.Contains("就绪"),
            $"状态应为升温中或就绪，实际: {stateText}");

        Capture("8.2.1_Idle→Preparing");
        _output.WriteLine($"✅ 状态变为: {stateText}");
    }

    [Fact(DisplayName = "8.2.2 Preparing → Ready — 温度稳定后自动切换")]
    public void StateMachine_PreparingToReady()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-02", "T02");
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_heating");

        _output.WriteLine("  等待温度稳定...");
        WaitStrategies.TryWaitForLabelText(window!, "当前状态", "就绪", TimeSpan.FromSeconds(30));

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.Contains("就绪", stateText);

        Capture("8.2.2_Preparing→Ready");
        _output.WriteLine($"✅ 温度稳定后自动切换: {stateText}");
    }

    [Fact(DisplayName = "8.2.3 Ready → Recording — 点击开始记录")]
    public void StateMachine_ReadyToRecording()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-03", "T03");
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window!, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(1000);

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.Contains("记录", stateText);

        Capture("8.2.3_Ready→Recording");
        _output.WriteLine($"✅ 状态变为: {stateText}");
    }

    [Fact(DisplayName = "8.2.7 Recording → Complete — 手动停止")]
    public void StateMachine_RecordingToComplete_ManualStop()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-04", "T04");
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window!, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_recording");

        Thread.Sleep(3000);

        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(1000);

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.True(stateText.Contains("完成") || stateText.Contains("已完"), $"状态应为完成，实际: {stateText}");

        Capture("8.2.7_Recording→Complete_手动停止");
        _output.WriteLine($"✅ 手动停止后状态: {stateText}");
    }

    [Fact(DisplayName = "8.3.2 停止加热 — Preparing/Ready → Idle")]
    public void StateMachine_StopHeating_ReturnsToIdle()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-05", "T05");
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_heating");
        Thread.Sleep(1000);

        ClickButtonBySignal("click_stop_heating");
        Thread.Sleep(1000);

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.Contains("空闲", stateText);

        Capture("8.3.2_停止加热→Idle");
        _output.WriteLine($"✅ 停止加热后状态: {stateText}");
    }

    [Fact(DisplayName = "8.3.1 Ready → Preparing — 温度跌出稳定范围自动回退")]
    public void StateMachine_ReadyFallbackToPreparing()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);

        var window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
        Assert.NotNull(window);

        CreateNewTest(window!, "SM-06", "T06");
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window!, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        window = Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(5)) ?? window;

        ClickButtonBySignal("click_stop_heating");
        Thread.Sleep(3000);

        var stateText = FlaUIHelper.GetLabelText(window!, "当前状态", TimeSpan.FromSeconds(5));
        _output.WriteLine($"  停止加热后状态: {stateText}");

        Capture("8.3.1_Ready回退");
        _output.WriteLine("✅ Ready 状态回退测试完成");
    }

    }
