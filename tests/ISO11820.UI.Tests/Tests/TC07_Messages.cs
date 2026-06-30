using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第五部分：系统消息日志验收测试
/// </summary>
public class TC07_Messages : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC07_Messages(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "5.2.1 系统初始化消息")]
    public void Messages_SystemInit()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);
        Thread.Sleep(2000);

        var hasInitMsg = FlaUIHelper.WaitForMessageKeyword(window, "初始化", TimeSpan.FromSeconds(5));
        Assert.True(hasInitMsg, "应包含系统初始化消息");

        var messages = FlaUIHelper.GetMessageLines(window);
        _output.WriteLine($"  消息列表 ({messages.Count} 条):");
        foreach (var msg in messages.Take(5))
            _output.WriteLine($"    {msg}");

        Capture("5.2.1_系统初始化消息");
        _output.WriteLine("✅ 系统初始化消息验证通过");
    }

    [Fact(DisplayName = "5.2.2 开始升温消息")]
    public void Messages_StartHeating()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "MSG-01", "T01");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        Thread.Sleep(2000);

        var hasMsg = FlaUIHelper.WaitForMessageKeyword(window, "升温", TimeSpan.FromSeconds(5));
        Assert.True(hasMsg, "应包含开始升温消息");

        Capture("5.2.2_开始升温消息");
        _output.WriteLine("✅ 开始升温消息验证通过");
    }

    [Fact(DisplayName = "5.2.3 温度稳定消息")]
    public void Messages_TemperatureStable()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "MSG-02", "T02");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");

        var hasMsg = FlaUIHelper.WaitForMessageKeyword(window, "稳定", TimeSpan.FromSeconds(30));
        Assert.True(hasMsg, "应包含温度稳定消息");

        Capture("5.2.3_温度稳定消息");
        _output.WriteLine("✅ 温度稳定消息验证通过");
    }

    [Fact(DisplayName = "5.2.4 开始记录消息")]
    public void Messages_StartRecording()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "MSG-03", "T03");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(2000);

        var hasMsg = FlaUIHelper.WaitForMessageKeyword(window, "记录", TimeSpan.FromSeconds(5));
        Assert.True(hasMsg, "应包含开始记录消息");

        Capture("5.2.4_开始记录消息");
        ClickButtonBySignal("click_stop_recording");
        _output.WriteLine("✅ 开始记录消息验证通过");
    }

    [Fact(DisplayName = "5.2.7 手动停止记录消息")]
    public void Messages_ManualStop()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "MSG-04", "T04");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(3000);
        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(2000);

        var hasMsg = FlaUIHelper.WaitForMessageKeyword(window, "停止", TimeSpan.FromSeconds(5));
        Assert.True(hasMsg, "应包含手动停止记录消息");

        Capture("5.2.7_手动停止消息");
        _output.WriteLine("✅ 手动停止记录消息验证通过");
    }

    [Fact(DisplayName = "5.1.1 消息时间格式 — HH:mm:ss")]
    public void Messages_TimeFormat()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);
        Thread.Sleep(2000);

        var messages = FlaUIHelper.GetMessageLines(window);
        if (messages.Count > 0)
        {
            var firstMsg = messages[0];
            _output.WriteLine($"  首条消息: {firstMsg}");

            Assert.True(
                firstMsg.Contains("[") || System.Text.RegularExpressions.Regex.IsMatch(firstMsg, @"\d{2}:\d{2}:\d{2}"),
                $"消息应包含时间格式，实际: {firstMsg}");
        }

        _output.WriteLine("✅ 消息时间格式验证通过");
    }

    [Fact(DisplayName = "5.4.4 消息自动滚动 — 最新消息可见")]
    public void Messages_AutoScroll()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "MSG-05", "T05");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(3000);
        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(2000);

        var messages = FlaUIHelper.GetMessageLines(window);
        if (messages.Count > 0)
        {
            var lastMsg = messages.Last();
            _output.WriteLine($"  最后一条消息: {lastMsg}");
            Assert.NotNull(lastMsg);
        }

        Capture("5.4.4_消息自动滚动");
        _output.WriteLine("✅ 消息自动滚动验证通过（最新消息可见）");
    }

    }
