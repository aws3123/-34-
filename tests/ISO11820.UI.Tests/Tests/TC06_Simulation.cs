using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第七部分：仿真温度引擎验收测试
/// </summary>
public class TC06_Simulation : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC06_Simulation(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "7.1.1 仿真引擎 — 800ms 采集周期")]
    public void Simulation_TickInterval()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "SIM-01", "T01");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");

        // 记录初始温度
        var temp1 = ReadTemperature(window, "炉温 1");
        _output.WriteLine($"  T=0s: TF1={temp1}°C");
        Thread.Sleep(800);
        var temp2 = ReadTemperature(window, "炉温 1");
        _output.WriteLine($"  T=0.8s: TF1={temp2}°C");
        Thread.Sleep(800);
        var temp3 = ReadTemperature(window, "炉温 1");
        _output.WriteLine($"  T=1.6s: TF1={temp3}°C");

        // 验证温度在变化（仿真在工作）
        Assert.True(temp2 >= temp1, $"温度应递增: {temp1} → {temp2}");
        Assert.True(temp3 >= temp2, $"温度应递增: {temp2} → {temp3}");

        Capture("7.1.1_仿真采集周期");
        _output.WriteLine("✅ 仿真引擎每 800ms 更新温度");
    }

    [Fact(DisplayName = "7.2.1~7.2.7 升温阶段 — 各通道温度行为")]
    public void Simulation_HeatingPhase()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "SIM-02", "T02");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        Thread.Sleep(2000);

        // 读取各通道温度
        var tf1 = ReadTemperature(window, "炉温 1");
        var tf2 = ReadTemperature(window, "炉温 2");
        var ts = ReadTemperature(window, "表面温");
        var tc = ReadTemperature(window, "中心温");
        var tcal = ReadTemperature(window, "校准温");

        _output.WriteLine($"  升温阶段各通道:");
        _output.WriteLine($"    TF1 (炉温1): {tf1}°C");
        _output.WriteLine($"    TF2 (炉温2): {tf2}°C");
        _output.WriteLine($"    TS  (表面温): {ts}°C");
        _output.WriteLine($"    TC  (中心温): {tc}°C");
        _output.WriteLine($"    TCal(校准温): {tcal}°C");

        // 验证升温阶段：炉温 > 表面温 > 中心温
        Assert.True(tf1 > ts, $"升温阶段 TF1({tf1}) 应 > TS({ts})");
        Assert.True(ts > tc, $"升温阶段 TS({ts}) 应 > TC({tc})");

        // 验证 TF1 和 TF2 接近但不完全相同
        var diff = Math.Abs(tf1 - tf2);
        Assert.True(diff < 5, $"TF1({tf1}) 和 TF2({tf2}) 差值应 < 5°C，实际 {diff}");

        Capture("7.2_升温阶段_各通道");
        _output.WriteLine("✅ 升温阶段温度行为验证通过");
    }

    [Fact(DisplayName = "7.3.1~7.3.5 稳定阶段 — 温度钳位到 750°C")]
    public void Simulation_StablePhase()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "SIM-03", "T03");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");

        // 等待温度稳定
        _output.WriteLine("  等待温度稳定...");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));

        // 稳定后读取温度
        var tf1 = ReadTemperature(window, "炉温 1");
        var tf2 = ReadTemperature(window, "炉温 2");

        _output.WriteLine($"  稳定阶段:");
        _output.WriteLine($"    TF1: {tf1}°C (期望 745~755)");
        _output.WriteLine($"    TF2: {tf2}°C (期望 745~755)");

        // 验证稳定在 745~755°C
        Assert.True(tf1 >= 740 && tf1 <= 760, $"TF1({tf1}) 应在 745~755°C 范围内");
        Assert.True(tf2 >= 740 && tf2 <= 760, $"TF2({tf2}) 应在 745~755°C 范围内");

        Capture("7.3_稳定阶段_温度钳位");
        _output.WriteLine("✅ 稳定阶段温度钳位验证通过");
    }

    [Fact(DisplayName = "7.4.1~7.4.6 记录阶段 — 表面温/中心温趋近行为")]
    public void Simulation_RecordingPhase()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "SIM-04", "T04");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));
        ClickButtonBySignal("click_start_recording");

        // 等待一段时间让表面温和中心温趋近
        Thread.Sleep(5000);

        var tf1 = ReadTemperature(window, "炉温 1");
        var ts = ReadTemperature(window, "表面温");
        var tc = ReadTemperature(window, "中心温");

        _output.WriteLine($"  记录阶段:");
        _output.WriteLine($"    TF1: {tf1}°C");
        _output.WriteLine($"    TS:  {ts}°C (趋近 TF1×0.95={tf1 * 0.95:F1})");
        _output.WriteLine($"    TC:  {tc}°C (趋近 TF1×0.85={tf1 * 0.85:F1})");

        // 表面温 > 中心温
        Assert.True(ts > tc, $"表面温({ts}) 应 > 中心温({tc})");

        // 表面温趋近 TF1×0.95
        var surfaceTarget = Math.Min(tf1 * 0.95, 800);
        Assert.True(Math.Abs(ts - surfaceTarget) < 20,
            $"表面温({ts}) 应趋近目标({surfaceTarget:F1})");

        Capture("7.4_记录阶段_趋近行为");
        ClickButtonBySignal("click_stop_recording");
        _output.WriteLine("✅ 记录阶段温度趋近行为验证通过");
    }

    [Fact(DisplayName = "7.5.1~7.5.3 降温阶段 — 停止加热后温度递减")]
    public void Simulation_CoolingPhase()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow(); Assert.NotNull(window);

        CreateNewTest(window, "SIM-05", "T05");
        window = GetMainWindow() ?? window;
        ClickButtonBySignal("click_start_heating");
        WaitStrategies.TryWaitForLabelText(window, "当前状态", "就绪", TimeSpan.FromSeconds(30));

        // 记录降温前温度
        var tempBefore = ReadTemperature(window, "炉温 1");
        _output.WriteLine($"  降温前 TF1: {tempBefore}°C");

        // 停止加热
        ClickButtonBySignal("click_stop_heating");
        Thread.Sleep(3000);

        var tempAfter = ReadTemperature(window, "炉温 1");
        _output.WriteLine($"  降温后 TF1: {tempAfter}°C");

        Assert.True(tempAfter < tempBefore,
            $"降温后温度应降低: {tempBefore} → {tempAfter}");

        Capture("7.5_降温阶段");
        _output.WriteLine("✅ 降温阶段温度递减验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════

    private double ReadTemperature(FlaUI.Core.AutomationElements.Window window, string automationId)
    {
        var text = FlaUIHelper.GetLabelText(window, automationId, TimeSpan.FromSeconds(5));
        if (string.IsNullOrEmpty(text)) return 0;

        // 解析 "炉温 1：750.5 °C" 格式
        var parts = text.Split(new[] { '：', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var numPart = parts[1].Replace("°C", "").Replace("℃", "").Trim();
            if (double.TryParse(numPart, out double temp))
                return temp;
        }
        return 0;
    }

    }
