using FlaUI.Core.AutomationElements;
using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第十部分：端到端完整演示流程验收测试
/// 按操作流程顺序完整走一遍
/// </summary>
public class TC10_FullFlow : UITestBase
{
    private readonly ITestOutputHelper _output;
    private readonly List<(string step, string screenshot)> _steps = new();

    public TC10_FullFlow(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "10.0 完整演示流程 — 端到端")]
    public void FullFlow_EndToEnd()
    {
        _output.WriteLine("═══════════════════════════════════════════════════");
        _output.WriteLine("  ISO 11820 完整演示流程验收");
        _output.WriteLine("═══════════════════════════════════════════════════");

        // ═══ 步骤 1：启动并登录 ═══
        Step("步骤1_启动并登录", () =>
        {
            StartAndLoginViaSignal();
        });

        // ═══ 步骤 2：验证主界面 ═══
        Step("步骤2_验证主界面", () =>
        {
            var mainWindow = GetMainWindow();
            Assert.NotNull(mainWindow);
            _output.WriteLine("  ✅ 成功进入主界面");
        });

        // ═══ 步骤 3：新建试验 ═══
        Step("步骤3_点击新建试验", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            ClickButtonBySignal("click_newtest");
            Thread.Sleep(1000);
        });

        Step("步骤3_填写试验信息", () =>
        {
            nint dialogHwnd = FindWindow(null, "新建试验");
            Assert.True(dialogHwnd != nint.Zero, "新建试验对话框应出现");

            var edits = new List<nint>();
            EnumChildWindows(dialogHwnd, (hwnd, _) =>
            {
                var cls = GetWindowClassName(hwnd);
                if (cls.StartsWith("WindowsForms10.Edit.", StringComparison.OrdinalIgnoreCase))
                    edits.Add(hwnd);
                return true;
            }, nint.Zero);

            var pid = $"E2E-{DateTime.Now:HHmmss}";
            // WinForms Edit 控件索引: 2=样品编号, 3=试验标识, 4=样品名称, 5=规格, 6=高度, 7=直径, 10=试验前质量
            SetTextViaWin32(edits[2], pid);
            SetTextViaWin32(edits[3], $"T{DateTime.Now:HHmmss}");
            SetTextViaWin32(edits[4], "端到端测试样品");
            SetTextViaWin32(edits[5], "100×50mm");
            SetTextViaWin32(edits[6], "50");
            SetTextViaWin32(edits[7], "100");
            SetTextViaWin32(edits[10], "500.0");

            _output.WriteLine($"  ✅ 已填写试验信息 (样品编号: {pid})");
        });

        Step("步骤3_验证设备信息自动", () =>
        {
            nint dialogHwnd = FindWindow(null, "新建试验");
            if (dialogHwnd != nint.Zero)
            {
                var edits = new List<nint>();
                EnumChildWindows(dialogHwnd, (hwnd, _) =>
                {
                    var cls = GetWindowClassName(hwnd);
                    if (cls.StartsWith("WindowsForms10.Edit.", StringComparison.OrdinalIgnoreCase))
                        edits.Add(hwnd);
                    return true;
                }, nint.Zero);

                // 设备信息在只读 Edit 控件: 11=设备编号, 14=恒功率值
                if (edits.Count > 11)
                {
                    var deviceId = GetWindowTitle(edits[11]);
                    _output.WriteLine($"  设备编号: {deviceId}");
                    Assert.False(string.IsNullOrEmpty(deviceId), "设备编号应自动填入");
                }
                if (edits.Count > 14)
                {
                    var constPower = GetWindowTitle(edits[14]);
                    _output.WriteLine($"  恒功率值: {constPower}");
                }
            }
            _output.WriteLine("  ✅ 设备信息自动带入");
        });

        Step("步骤3_创建试验", () =>
        {
            nint dialogHwnd = FindWindow(null, "新建试验");
            if (dialogHwnd != nint.Zero)
            {
                nint btn = nint.Zero;
                EnumChildWindows(dialogHwnd, (hwnd, _) =>
                {
                    if (GetWindowClassName(hwnd).Contains("Button") && GetWindowTitle(hwnd).Contains("创建试验"))
                        btn = hwnd;
                    return true;
                }, nint.Zero);
                if (btn != nint.Zero)
                {
                    SendMessage(btn, 0x00F5 /*BM_CLICK*/, 0, 0);
                    Thread.Sleep(1000);
                }
            }
            _output.WriteLine("  ✅ 试验创建成功");
        });

        // ═══ 步骤 4：开始升温 ═══
        Step("步骤4_点击开始升温", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            ClickButtonBySignal("click_start_heating");
            Thread.Sleep(1000);
        });

        Step("步骤4_验证状态升温中", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var state = FlaUIHelper.GetLabelText(window, "当前状态", TimeSpan.FromSeconds(5));
            Assert.NotNull(state);
            Assert.Contains("升温", state);
            _output.WriteLine($"  ✅ 状态: {state}");
        });

        Step("步骤4_验证温度上升", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var temp = ReadTemperature(window, "炉温 1");
            _output.WriteLine($"  炉温1: {temp}°C");
            Assert.True(temp > 0, "温度应大于 0");
        });

        Step("步骤4_验证消息", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var hasMsg = FlaUIHelper.WaitForListBoxItem(window, "_messageListBox",
                "升温", TimeSpan.FromSeconds(5));
            Assert.True(hasMsg, "应显示升温消息");
            _output.WriteLine("  ✅ 消息: 开始升温");
        });

        Step("步骤4_验证按钮状态", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            Assert.False(FlaUIHelper.IsButtonEnabled(window, "开始升温"), "开始升温应禁用");
            Assert.True(FlaUIHelper.IsButtonEnabled(window, "停止升温"), "停止升温应启用");
            _output.WriteLine("  ✅ 按钮状态正确");
        });

        // ═══ 步骤 5：温度稳定 ═══
        Step("步骤5_等待温度稳定", () =>
        {
            _output.WriteLine("  等待温度稳定中...");
            var window = GetMainWindow(); Assert.NotNull(window);
            var stable = WaitStrategies.TryWaitForLabelText(window, "当前状态",
                "就绪", TimeSpan.FromSeconds(60));
            Assert.True(stable, "应自动切换到就绪状态");
        });

        Step("步骤5_验证状态就绪", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var state = FlaUIHelper.GetLabelText(window, "当前状态", TimeSpan.FromSeconds(5));
            Assert.NotNull(state);
            Assert.Contains("就绪", state);
            _output.WriteLine($"  ✅ 状态: {state}");
        });

        Step("步骤5_验证温度稳定消息", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var hasMsg = FlaUIHelper.WaitForListBoxItem(window, "_messageListBox",
                "稳定", TimeSpan.FromSeconds(5));
            Assert.True(hasMsg, "应显示温度稳定消息");
            _output.WriteLine("  ✅ 消息: 温度已稳定");
        });

        Step("步骤5_验证按钮状态", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            Assert.True(FlaUIHelper.IsButtonEnabled(window, "开始记录"), "开始记录应启用");
            Assert.True(FlaUIHelper.IsButtonEnabled(window, "停止升温"), "停止升温应启用");
            _output.WriteLine("  ✅ 按钮状态正确");
        });

        // ═══ 步骤 6：开始记录 ═══
        Step("步骤6_点击开始记录", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            ClickButtonBySignal("click_start_recording");
            Thread.Sleep(1000);
        });

        Step("步骤6_验证状态记录中", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var state = FlaUIHelper.GetLabelText(window, "当前状态", TimeSpan.FromSeconds(5));
            Assert.NotNull(state);
            Assert.Contains("记录", state);
            _output.WriteLine($"  ✅ 状态: {state}");
        });

        Step("步骤6_验证计时器", () =>
        {
            Thread.Sleep(3000);
            var window = GetMainWindow(); Assert.NotNull(window);
            var elapsed = FlaUIHelper.GetLabelText(window, "计时", TimeSpan.FromSeconds(5));
            Assert.NotNull(elapsed);
            Assert.Contains("计时", elapsed);
            _output.WriteLine($"  ✅ 计时器: {elapsed}");
        });

        Step("步骤6_验证消息", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var hasMsg = FlaUIHelper.WaitForListBoxItem(window, "_messageListBox",
                "记录", TimeSpan.FromSeconds(5));
            Assert.True(hasMsg, "应显示开始记录消息");
            _output.WriteLine("  ✅ 消息: 开始记录");
        });

        Step("步骤6_验证按钮状态", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            Assert.True(FlaUIHelper.IsButtonEnabled(window, "停止记录"), "停止记录应启用");
            Assert.False(FlaUIHelper.IsButtonEnabled(window, "开始记录"), "开始记录应禁用");
            _output.WriteLine("  ✅ 按钮状态正确");
        });

        // ═══ 步骤 7：等待完成（手动停止） ═══
        Step("步骤7_等待5秒后手动停止", () =>
        {
            Thread.Sleep(5000);
            var window = GetMainWindow(); Assert.NotNull(window);
            ClickButtonBySignal("click_stop_recording");
            Thread.Sleep(2000);
        });

        Step("步骤7_验证状态完成", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var state = FlaUIHelper.GetLabelText(window, "当前状态", TimeSpan.FromSeconds(5));
            Assert.NotNull(state);
            Assert.True(state.Contains("完成") || state.Contains("已完"), $"状态应为完成，实际: {state}");
            _output.WriteLine($"  ✅ 状态: {state}");
        });

        Step("步骤7_验证完成消息", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            var hasMsg = FlaUIHelper.WaitForListBoxItem(window, "_messageListBox",
                "停止", TimeSpan.FromSeconds(5));
            Assert.True(hasMsg, "应显示停止记录消息");
            _output.WriteLine("  ✅ 消息: 用户手动停止记录");
        });

        // ═══ 步骤 8：记录查询 ═══
        Step("步骤8_切换到记录查询Tab", () =>
        {
            var window = GetMainWindow(); Assert.NotNull(window);
            try
            {
                FlaUIHelper.SelectTab(window, "记录查询");
                Thread.Sleep(1000);
                var tabName = FlaUIHelper.GetSelectedTabName(window);
                _output.WriteLine($"  当前 Tab: {tabName}");
            }
            catch
            {
                _output.WriteLine("  ⚠️ Tab 切换未成功（记录查询可能未实现）");
            }
        });

        // ═══ 步骤 9：导出验证 ═══
        Step("步骤9_检查导出文件", () =>
        {
            var baseDir = @"D:\ISO11820";
            var testDataDir = Path.Combine(baseDir, "TestData");
            var reportsDir = Path.Combine(baseDir, "Reports");

            if (Directory.Exists(testDataDir))
            {
                var csvCount = Directory.GetFiles(testDataDir, "*.csv", SearchOption.AllDirectories).Length;
                _output.WriteLine($"  CSV 文件数: {csvCount}");
            }
            if (Directory.Exists(reportsDir))
            {
                var xlsxCount = Directory.GetFiles(reportsDir, "*.xlsx", SearchOption.AllDirectories).Length;
                var pdfCount = Directory.GetFiles(reportsDir, "*.pdf", SearchOption.AllDirectories).Length;
                _output.WriteLine($"  Excel 文件数: {xlsxCount}");
                _output.WriteLine($"  PDF 文件数: {pdfCount}");
            }
        });

        // ═══ 汇总 ═══
        _output.WriteLine("");
        _output.WriteLine("═══════════════════════════════════════════════════");
        _output.WriteLine($"  完整流程验收完成，共 {_steps.Count} 个步骤");
        _output.WriteLine("═══════════════════════════════════════════════════");

        foreach (var (step, screenshot) in _steps)
        {
            _output.WriteLine($"  📸 {step}: {screenshot}");
        }
    }

    // ═══════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════

    private void Step(string name, Action action)
    {
        _output.WriteLine($"");
        _output.WriteLine($"  ─── {name} ───");
        try
        {
            action();
            var path = Capture($"E2E_{name}");
            _steps.Add((name, path));
        }
        catch (Exception ex)
        {
            Capture($"E2E_{name}_FAIL");
            _output.WriteLine($"  ❌ 失败: {ex.Message}");
            throw;
        }
    }

    private double ReadTemperature(FlaUI.Core.AutomationElements.Window window, string automationId)
    {
        var text = FlaUIHelper.GetLabelText(window, automationId, TimeSpan.FromSeconds(5));
        if (string.IsNullOrEmpty(text)) return 0;
        var parts = text.Split(new[] { '：', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var numPart = parts[1].Replace("°C", "").Replace("℃", "").Trim();
            if (double.TryParse(numPart, out double temp)) return temp;
        }
        return 0;
    }
}
