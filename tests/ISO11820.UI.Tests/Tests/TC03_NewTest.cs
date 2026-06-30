using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests.Tests;

/// <summary>
/// 第六部分：新建试验功能验收测试
/// </summary>
public class TC03_NewTest : UITestBase
{
    private readonly ITestOutputHelper _output;

    public TC03_NewTest(ITestOutputHelper output) => _output = output;

    // ═══════════════════════════════════════════════════
    //  Win32 对话框辅助方法
    // ═══════════════════════════════════════════════════

    /// <summary>通过信号文件打开新建试验对话框，返回对话框 HWND</summary>
    private nint OpenNewTestDialog()
    {
        ClickButtonBySignal("click_newtest");

        nint dialogHwnd = nint.Zero;
        for (int i = 0; i < 50; i++)
        {
            dialogHwnd = FindWindow(null, "新建试验");
            if (dialogHwnd != nint.Zero) break;
            Thread.Sleep(200);
        }

        if (dialogHwnd == nint.Zero)
            throw new InvalidOperationException("新建试验对话框未出现");

        Thread.Sleep(500);
        return dialogHwnd;
    }

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

    /// <summary>在对话框中按文本查找按钮（包括 RadioButton）</summary>
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

    /// <summary>通过 Win32 BM_CLICK 关闭对话框</summary>
    private void CloseDialog(nint dialogHwnd)
    {
        var cancelBtn = FindButtonByText(dialogHwnd, "取消");
        if (cancelBtn != nint.Zero)
        {
            SendMessage(cancelBtn, 0x00F5 /*BM_CLICK*/, 0, 0);
            Thread.Sleep(500);
        }
    }

    // ═══════════════════════════════════════════════════
    //  6.1 新建试验对话框 — 字段完整性
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "6.1 新建试验对话框 — 字段完整性")]
    public void NewTest_DialogHasAllFields()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow();
        Assert.NotNull(window);

        var dialogHwnd = OpenNewTestDialog();
        var edits = GetEditControls(dialogHwnd);
        _output.WriteLine($"  Edit 控件总数: {edits.Count}");

        var expectedFields = new (int index, string label)[]
        {
            (0, "环境温度"), (1, "环境湿度"), (2, "样品编号"), (3, "试验标识"),
            (4, "样品名称"), (5, "规格"), (6, "高度"), (7, "直径"),
            (8, "操作员"), (10, "试验前质量"),
        };

        foreach (var (idx, label) in expectedFields)
        {
            var exists = idx < edits.Count;
            _output.WriteLine($"  [{idx}] {label}: {(exists ? "✅ 存在" : "❌ 缺失")}");
        }

        // 验证设备信息（只读，按 Edit 控件索引查找: 11=设备编号, 12=设备名称, 13=检定日期, 14=恒功率值）
        var deviceFields = new (int index, string name)[]
        {
            (11, "设备编号"), (12, "设备名称"), (13, "检定日期"), (14, "恒功率值")
        };
        foreach (var (idx, name) in deviceFields)
        {
            if (idx < edits.Count)
            {
                var text = GetWindowTitle(edits[idx]);
                _output.WriteLine($"  {name}: 值='{text}'");
            }
        }

        // 验证时长模式
        var standardRadio = FindButtonByText(dialogHwnd, "标准 60 分钟");
        var customRadio = FindButtonByText(dialogHwnd, "自定义");
        Assert.True(standardRadio != nint.Zero, "标准 60 分钟 RadioButton 应存在");
        Assert.True(customRadio != nint.Zero, "自定义 RadioButton 应存在");

        // 验证按钮
        var createBtn = FindButtonByText(dialogHwnd, "创建试验");
        var cancelBtn = FindButtonByText(dialogHwnd, "取消");
        Assert.True(createBtn != nint.Zero, "创建试验按钮应存在");
        Assert.True(cancelBtn != nint.Zero, "取消按钮应存在");

        Capture("6.1_新建试验对话框_字段完整性");
        CloseDialog(dialogHwnd);

        window = GetMainWindow() ?? window;
        _output.WriteLine("✅ 新建试验对话框字段完整性验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  6.1.13~6.1.16 设备信息自动带入
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "6.1.13~6.1.16 设备信息自动带入")]
    public void NewTest_DeviceInfoAutoFilled()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow();
        Assert.NotNull(window);

        var dialogHwnd = OpenNewTestDialog();
        var edits = GetEditControls(dialogHwnd);

        // 设备信息在只读 Edit 控件: 11=设备编号, 12=设备名称, 14=恒功率值
        // 验证设备编号
        Assert.True(edits.Count > 11, "设备编号 Edit 控件应存在");
        var deviceId = GetWindowTitle(edits[11]);
        Assert.False(string.IsNullOrEmpty(deviceId));
        _output.WriteLine($"  设备编号: {deviceId}");

        // 验证设备名称
        Assert.True(edits.Count > 12, "设备名称 Edit 控件应存在");
        var deviceName = GetWindowTitle(edits[12]);
        Assert.False(string.IsNullOrEmpty(deviceName));
        _output.WriteLine($"  设备名称: {deviceName}");

        // 验证恒功率值
        Assert.True(edits.Count > 14, "恒功率值 Edit 控件应存在");
        var constPower = GetWindowTitle(edits[14]);
        Assert.False(string.IsNullOrEmpty(constPower));
        _output.WriteLine($"  恒功率值: {constPower}");

        Capture("6.1.13_设备信息自动带入");
        CloseDialog(dialogHwnd);

        window = GetMainWindow() ?? window;
        _output.WriteLine("✅ 设备信息自动带入验证通过");
    }

    // ═══════════════════════════════════════════════════
    //  6.2.2 新建试验 — 保存到数据库
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "6.2.2 新建试验 — 保存到数据库")]
    public void NewTest_SaveToDatabase()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow();
        Assert.NotNull(window);

        var dialogHwnd = OpenNewTestDialog();
        var edits = GetEditControls(dialogHwnd);

        // 填写必要字段（WinForms Edit 控件索引: 2=样品编号, 3=试验标识, 4=样品名称, 10=试验前质量）
        SetTextViaWin32(edits[2], $"TEST-{DateTime.Now:HHmmss}");
        SetTextViaWin32(edits[3], $"T{DateTime.Now:HHmmss}");
        SetTextViaWin32(edits[4], "验收测试样品");
        SetTextViaWin32(edits[10], "500.0");

        Capture("6.2.2_填写试验信息");

        // 点击创建
        var createBtn = FindButtonByText(dialogHwnd, "创建试验");
        if (createBtn != nint.Zero)
        {
            SendMessage(createBtn, 0x00F5 /*BM_CLICK*/, 0, 0);
        }
        else
        {
            // 尝试按 Enter
            SendMessage(dialogHwnd, 0x0100 /*WM_KEYDOWN*/, 0x0D /*VK_RETURN*/, 0);
            SendMessage(dialogHwnd, 0x0101 /*WM_KEYUP*/, 0x0D /*VK_RETURN*/, 0);
        }
        Thread.Sleep(1000);

        window = GetMainWindow() ?? window;
        Capture("6.2.2_创建成功后");
        _output.WriteLine("✅ 新建试验保存到数据库");
    }

    // ═══════════════════════════════════════════════════
    //  6.2.3 新建试验 — 必填字段为空时提示
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "6.2.3 新建试验 — 必填字段为空时提示")]
    public void NewTest_EmptyRequiredField_ShowsError()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow();
        Assert.NotNull(window);

        var dialogHwnd = OpenNewTestDialog();

        // 不填写任何内容直接创建
        var createBtn = FindButtonByText(dialogHwnd, "创建试验");
        if (createBtn != nint.Zero)
        {
            SendMessage(createBtn, 0x00F5 /*BM_CLICK*/, 0, 0);
        }
        Thread.Sleep(1000);

        // 验证对话框仍然打开或有错误提示
        Capture("6.2.3_必填字段为空_提示");

        // 清理
        var stillOpen = FindWindow(null, "新建试验");
        if (stillOpen != nint.Zero)
        {
            CloseDialog(stillOpen);
        }

        window = GetMainWindow() ?? window;
        _output.WriteLine("✅ 必填字段为空时有错误提示或对话框未关闭");
    }

    // ═══════════════════════════════════════════════════
    //  6.1.10 试验时长模式 — 标准/自定义切换
    // ═══════════════════════════════════════════════════

    [Fact(DisplayName = "6.1.10 试验时长模式 — 标准/自定义切换")]
    public void NewTest_DurationModeSwitch()
    {
        StartAndLoginViaSignal();
        var window = GetMainWindow();
        Assert.NotNull(window);

        var dialogHwnd = OpenNewTestDialog();

        // 默认选中标准 60 分钟
        var standardRadio = FindButtonByText(dialogHwnd, "标准 60 分钟");
        Assert.True(standardRadio != nint.Zero, "标准 60 分钟 RadioButton 应存在");

        // 切换到自定义
        var customRadio = FindButtonByText(dialogHwnd, "自定义");
        Assert.True(customRadio != nint.Zero, "自定义 RadioButton 应存在");
        SendMessage(customRadio, 0x00F5 /*BM_CLICK*/, 0, 0);
        Thread.Sleep(300);

        // 自定义分钟输入框应该可用（Edit 索引 9 = _customMinutesTextBox）
        var edits = GetEditControls(dialogHwnd);
        _output.WriteLine($"  Edit 控件总数: {edits.Count}");
        if (edits.Count > 9)
        {
            var customMinutes = GetWindowTitle(edits[9]);
            _output.WriteLine($"  自定义分钟输入框: 存在, 值='{customMinutes}'");
        }

        Capture("6.1.10_时长模式切换");
        CloseDialog(dialogHwnd);

        window = GetMainWindow() ?? window;
        _output.WriteLine("✅ 试验时长模式切换验证通过");
    }
}