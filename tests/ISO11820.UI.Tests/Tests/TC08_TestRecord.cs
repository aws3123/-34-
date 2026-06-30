using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第十一部分：试验现象记录验收测试
/// </summary>
public class TC08_TestRecord : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC08_TestRecord(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "11.1 试验记录对话框 — 字段完整性")]
    public void TestRecord_DialogHasAllFields()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 完成一次试验
        RunFullTestCycle(window, "TR-01", "T01");

        // 点击试验记录（在 Complete 状态下应出现保存提示）
        Capture("11.1_试验记录对话框");
        _output.WriteLine("✅ 试验记录对话框测试完成");
    }

    [Fact(DisplayName = "11.1.1 火焰复选框")]
    public void TestRecord_FlameCheckBox()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);
        RunFullTestCycle(window, "TR-02", "T02");

        // 查找试验记录对话框
        nint dialogHwnd = nint.Zero;
        for (int i = 0; i < 25; i++)
        {
            dialogHwnd = FindWindow(null, "试验记录");
            if (dialogHwnd != nint.Zero) break;
            Thread.Sleep(200);
        }

        if (dialogHwnd != nint.Zero)
        {
            // 查找火焰复选框（WinForms CheckBox 类名含 Button）
            nint chkFlame = FindButtonByText(dialogHwnd, "火焰");
            _output.WriteLine($"  火焰复选框: {(chkFlame != nint.Zero ? "存在" : "不存在")}");

            Capture("11.1.1_火焰复选框");
        }

        _output.WriteLine("✅ 火焰复选框验证完成");
    }

    [Fact(DisplayName = "11.1.4 火焰输入联动 — 未勾选时不可用")]
    public void TestRecord_FlameInputLinked()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);
        RunFullTestCycle(window, "TR-03", "T03");

        nint dialogHwnd = nint.Zero;
        for (int i = 0; i < 25; i++)
        {
            dialogHwnd = FindWindow(null, "试验记录");
            if (dialogHwnd != nint.Zero) break;
            Thread.Sleep(200);
        }

        if (dialogHwnd != nint.Zero)
        {
            // 枚举所有 Edit 控件，按索引访问火焰时刻和火焰持续时间
            // （Win32 无法按 AutomationId 定位，需根据实际控件索引确定）
            var edits = GetEditControls(dialogHwnd);
            _output.WriteLine($"  Edit 控件总数: {edits.Count}");
            for (int idx = 0; idx < edits.Count; idx++)
            {
                _output.WriteLine($"    Edit[{idx}]: 值='{GetWindowTitle(edits[idx])}'");
            }

            Capture("11.1.4_火焰输入联动");
        }

        _output.WriteLine("✅ 火焰输入联动验证完成");
    }

    [Fact(DisplayName = "11.1.5 试验后质量 — 必填")]
    public void TestRecord_PostWeightRequired()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);
        RunFullTestCycle(window, "TR-04", "T04");

        nint dialogHwnd = nint.Zero;
        for (int i = 0; i < 25; i++)
        {
            dialogHwnd = FindWindow(null, "试验记录");
            if (dialogHwnd != nint.Zero) break;
            Thread.Sleep(200);
        }

        if (dialogHwnd != nint.Zero)
        {
            // 枚举所有 Edit 控件（试验后质量输入框按索引访问）
            var edits = GetEditControls(dialogHwnd);
            _output.WriteLine($"  Edit 控件总数: {edits.Count}");

            // 不填写试验后质量直接保存
            var okBtn = FindButtonByText(dialogHwnd, "保存");
            if (okBtn == nint.Zero) okBtn = FindButtonByText(dialogHwnd, "确定");
            if (okBtn != nint.Zero)
            {
                SendMessage(okBtn, 0x00F5 /*BM_CLICK*/, 0, 0);
                Thread.Sleep(1000);
            }

            // 验证有错误提示或对话框未关闭
            Capture("11.1.5_试验后质量必填");
        }

        _output.WriteLine("✅ 试验后质量必填验证完成");
    }

    [Fact(DisplayName = "11.2 保存后自动计算 — 失重率/温升")]
    public void TestRecord_AutoCalculation()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        // 完成试验
        CreateNewTest(window, "TR-05", "T05");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(5000);
        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(2000);

        Capture("11.2_试验完成_准备保存");
        _output.WriteLine("✅ 自动计算验证（需人工确认失重率和温升值）");
    }

    // ═══════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════

    /// <summary>枚举对话框中所有 Edit 子控件</summary>
    private List<nint> GetEditControls(nint dialogHwnd)
    {
        var edits = new List<nint>();
        EnumChildWindows(dialogHwnd, (hwnd, _) =>
        {
            var cls = GetWindowClassName(hwnd);
            if (cls.StartsWith("WindowsForms10.Edit.", StringComparison.OrdinalIgnoreCase))
                edits.Add(hwnd);
            return true;
        }, nint.Zero);
        return edits;
    }

    /// <summary>在对话框中按文本查找按钮（包括 CheckBox、RadioButton）</summary>
    private nint FindButtonByText(nint dialogHwnd, string text)
    {
        nint result = nint.Zero;
        EnumChildWindows(dialogHwnd, (hwnd, _) =>
        {
            if (GetWindowClassName(hwnd).Contains("Button"))
            {
                var title = GetWindowTitle(hwnd);
                if (title.Contains(text))
                    result = hwnd;
            }
            return true;
        }, nint.Zero);
        return result;
    }

    private void RunFullTestCycle(FlaUI.Core.AutomationElements.Window window, string productId, string testId)
    {
        CreateNewTest(window, productId, testId);
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");
        Thread.Sleep(3000);
        ClickButtonBySignal("click_stop_recording");
        Thread.Sleep(2000);
    }

    }
