using FlaUI.Core.Definitions;
using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第九部分：按钮状态控制验收测试
/// 验证每种状态下 6 个按钮的启用/禁用状态
/// </summary>
public class TC05_ButtonState : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC05_ButtonState(ITestOutputHelper output) => _output = output;

    // ═══════════════════════════════════════════════════
    //  9.1 Idle 状态按钮
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "9.1 Idle 状态 — 按钮状态矩阵")]
    public void ButtonState_Idle()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // Idle 状态下验证
        AssertButtonState(window, "新建试验", true, "9.1.1");
        AssertButtonState(window, "开始升温", true, "9.1.2");
        AssertButtonState(window, "停止升温", false, "9.1.3");
        AssertButtonState(window, "开始记录", false, "9.1.4");
        AssertButtonState(window, "停止记录", false, "9.1.5");
        AssertButtonState(window, "参数设置", true, "9.1.6");

        Capture("9.1_Idle状态_按钮矩阵");
        _output.WriteLine("✅ Idle 状态按钮矩阵验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  9.2 Preparing 状态按钮
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "9.2 Preparing 状态 — 按钮状态矩阵")]
    public void ButtonState_Preparing()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 进入 Preparing
        CreateNewTest(window, "BS-01", "T01");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        Thread.Sleep(1500);

        AssertButtonState(window, "新建试验", false, "9.2.1");
        AssertButtonState(window, "开始升温", false, "9.2.2");
        AssertButtonState(window, "停止升温", true, "9.2.3");
        AssertButtonState(window, "开始记录", false, "9.2.4");
        AssertButtonState(window, "停止记录", false, "9.2.5");
        AssertButtonState(window, "参数设置", false, "9.2.6");

        Capture("9.2_Preparing状态_按钮矩阵");
        _output.WriteLine("✅ Preparing 状态按钮矩阵验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  9.3 Ready 状态按钮
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "9.3 Ready 状态 — 按钮状态矩阵")]
    public void ButtonState_Ready()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 进入 Ready
        CreateNewTest(window, "BS-02", "T02");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        Thread.Sleep(500);

        AssertButtonState(window, "新建试验", false, "9.3.1");
        AssertButtonState(window, "开始升温", false, "9.3.2");
        AssertButtonState(window, "停止升温", true, "9.3.3");
        AssertButtonState(window, "开始记录", true, "9.3.4");
        AssertButtonState(window, "停止记录", false, "9.3.5");
        AssertButtonState(window, "参数设置", false, "9.3.6");

        Capture("9.3_Ready状态_按钮矩阵");
        _output.WriteLine("✅ Ready 状态按钮矩阵验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  9.4 Recording 状态按钮
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "9.4 Recording 状态 — 按钮状态矩阵")]
    public void ButtonState_Recording()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 进入 Recording
        CreateNewTest(window, "BS-03", "T03");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(1000);

        AssertButtonState(window, "新建试验", false, "9.4.1");
        AssertButtonState(window, "开始升温", false, "9.4.2");
        AssertButtonState(window, "停止升温", false, "9.4.3");
        AssertButtonState(window, "开始记录", false, "9.4.4");
        AssertButtonState(window, "停止记录", true, "9.4.5");
        AssertButtonState(window, "参数设置", false, "9.4.6");

        Capture("9.4_Recording状态_按钮矩阵");
        _output.WriteLine("✅ Recording 状态按钮矩阵验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  9.5 Complete 状态按钮
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "9.5 Complete 状态 — 按钮状态矩阵（未保存）")]
    public void ButtonState_Complete_Unsaved()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 进入 Complete
        CreateNewTest(window, "BS-04", "T04");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(3000);
        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(1000);

        // Complete + 未保存状态
        AssertButtonState(window, "新建试验", true, "9.5.1-未保存");
        AssertButtonState(window, "开始升温", false, "9.5.2");
        AssertButtonState(window, "停止升温", true, "9.5.3");
        AssertButtonState(window, "开始记录", false, "9.5.4");
        AssertButtonState(window, "停止记录", false, "9.5.5");
        AssertButtonState(window, "参数设置", true, "9.5.6");

        Capture("9.5_Complete状态_未保存");
        _output.WriteLine("✅ Complete 状态（未保存）按钮矩阵验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════

    private void AssertButtonState(FlaUI.Core.AutomationElements.Window window,
        string buttonName, bool expectedEnabled, string testCase)
    {
        var actualEnabled = FlaUIHelper.IsButtonEnabled(window, buttonName);
        var status = actualEnabled == expectedEnabled ? "✅" : "❌";
        var expected = expectedEnabled ? "启用" : "禁用";
        var actual = actualEnabled ? "启用" : "禁用";

        _output.WriteLine($"  {testCase} [{buttonName}]: 期望={expected}, 实际={actual} {status}");

        Assert.True(actualEnabled == expectedEnabled,
            $"按钮 '{buttonName}' 状态不符: 期望{expected}，实际{actual}");
    }

}
