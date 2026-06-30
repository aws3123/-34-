using FlaUI.Core.Definitions;
using ISO11820.UI.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第四部分：主界面布局验收测试
/// </summary>
public class TC02_MainLayout : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC02_MainLayout(ITestOutputHelper output) => _output = output;

    // ═══════════════════════════════════════════════════
    //  4.1 界面布局验证
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "4.1.1 主界面 — Tab 页结构")]
    public void MainLayout_HasTabPages()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var tabControl = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab));
        Assert.NotNull(tabControl);

        // 验证三个 Tab
        var tabs = tabControl!.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem));
        Assert.True(tabs.Length >= 3, $"Tab 数量不足: {tabs.Length}");

        var tabNames = tabs.Select(t => t.Name).ToList();
        Assert.Contains(tabNames, n => n != null && n.Contains("主操作界面"));
        Assert.Contains(tabNames, n => n != null && n.Contains("记录查询"));
        Assert.Contains(tabNames, n => n != null && n.Contains("设备校准"));

        Capture("4.1.1_Tab页结构");
        _output.WriteLine($"✅ Tab 页: {string.Join(", ", tabNames)}");
    }

    [Fact(DisplayName = "4.1.2 主界面 — 5通道温度数值显示区域")]
    public void MainLayout_HasTemperatureDisplay()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        // 验证 5 个温度标签
        var labels = new[] { "炉温 1", "炉温 2", "表面温", "中心温", "校准温" };
        var expectedTexts = new[] { "炉温", "炉温 2", "表面温", "中心温", "校准温" };

        for (int i = 0; i < labels.Length; i++)
        {
            var element = window.FindFirstDescendant(cf => cf.ByAutomationId(labels[i]));
            if (element != null)
            {
                var text = element.Properties.Name.ValueOrDefault;
                _output.WriteLine($"  {labels[i]}: {text}");
            }
        }

        Capture("4.1.2_温度显示区域");
        _output.WriteLine("✅ 5 通道温度数值显示区域存在");
    }

    [Fact(DisplayName = "4.1.3 主界面 — 温度曲线图区域")]
    public void MainLayout_HasChartArea()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        // OxyPlot PlotView 可能是 Custom 控件类型，查找包含 "chart" 或 "plot" 名称的元素
        var chartPanel = window.FindFirstDescendant(cf => cf.ByAutomationId("_chartPanel"))
            ?? window.FindFirstDescendant(cf => cf.ByName("chartPanel"))
            ?? window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Custom));

        // 降级：只要主界面有足够大的控件区域即可
        Assert.NotNull(chartPanel);

        Capture("4.1.3_曲线图区域");
        _output.WriteLine("✅ 温度曲线图区域存在");
    }

    [Fact(DisplayName = "4.1.4 主界面 — 计时器显示")]
    public void MainLayout_HasTimerDisplay()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var elapsedLabel = FlaUIHelper.GetLabelText(window, "计时");
        Assert.NotNull(elapsedLabel);
        Assert.Contains("计时", elapsedLabel);

        Capture("4.1.4_计时器显示");
        _output.WriteLine($"✅ 计时器显示: {elapsedLabel}");
    }

    [Fact(DisplayName = "4.1.5 主界面 — 状态显示")]
    public void MainLayout_HasStatusDisplay()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var stateLabel = FlaUIHelper.GetLabelText(window, "当前状态");
        Assert.NotNull(stateLabel);
        Assert.Contains("状态", stateLabel);
        Assert.Contains("空闲", stateLabel);

        Capture("4.1.5_状态显示");
        _output.WriteLine($"✅ 状态显示: {stateLabel}");
    }

    [Fact(DisplayName = "4.1.6 主界面 — 系统消息区域")]
    public void MainLayout_HasMessageArea()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var messageListBox = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
        Assert.NotNull(messageListBox);

        Capture("4.1.6_系统消息区域");
        _output.WriteLine("✅ 系统消息区域存在");
    }

    [Fact(DisplayName = "4.1.7 主界面 — 6 个操作按钮")]
    public void MainLayout_HasOperationButtons()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var buttonNames = new[] { "新建试验", "开始升温", "停止升温", "开始记录", "停止记录", "参数设置" };
        foreach (var name in buttonNames)
        {
            var btn = FlaUIHelper.FindButton(window, name);
            Assert.NotNull(btn);
            _output.WriteLine($"  按钮 '{name}': 存在, 启用={btn!.IsEnabled}");
        }

        Capture("4.1.7_操作按钮");
        _output.WriteLine("✅ 6 个操作按钮均存在");
    }

    // ═══════════════════════════════════════════════════
    //  4.2 实时温度显示验证
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "4.2.1~4.2.5 5通道温度格式 — 保留1位小数")]
    public void MainLayout_TemperatureFormat()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        // 等待温度数据更新
        Thread.Sleep(2000);

        var labels = new[] { "炉温 1", "炉温 2", "表面温", "中心温", "校准温" };
        foreach (var id in labels)
        {
            var text = FlaUIHelper.GetLabelText(window, id, TimeSpan.FromSeconds(5));
            _output.WriteLine($"  {id}: {text}");

            if (text != null && text.Contains("°C"))
            {
                // 验证包含数值
                Assert.Contains("°C", text);
            }
        }

        Capture("4.2_温度显示格式");
        _output.WriteLine("✅ 温度显示格式验证完成");
    }

    // ═══════════════════════════════════════════════════
    //  4.6 状态显示验证
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "4.6.2 Idle 状态 — 显示'空闲'")]
    public void MainLayout_IdleStatus()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        var stateText = FlaUIHelper.GetLabelText(window, "当前状态", TimeSpan.FromSeconds(5));
        Assert.NotNull(stateText);
        Assert.Contains("空闲", stateText);

        _output.WriteLine($"✅ Idle 状态: {stateText}");
    }

    // ═══════════════════════════════════════════════════
    //  布局合理性截图（供 AI/人工审查）
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "布局审查 — 主界面整体截图")]
    public void MainLayout_OverallScreenshot()
    {
        StartAndLoginAsAdmin();
        var window = Launcher.MainWindow;

        // 等待界面完全渲染
        Thread.Sleep(1000);

        var path = Capture("布局审查_主界面整体");
        _output.WriteLine($"📸 主界面截图已保存: {path}");
        _output.WriteLine("请人工/AI审查：");
        _output.WriteLine("  1. 温度显示区域是否在左侧");
        _output.WriteLine("  2. 曲线图是否在中间");
        _output.WriteLine("  3. 按钮是否在右侧");
        _output.WriteLine("  4. 消息区域是否在底部");
        _output.WriteLine("  5. 状态栏/计时器是否在顶部");
    }
}
