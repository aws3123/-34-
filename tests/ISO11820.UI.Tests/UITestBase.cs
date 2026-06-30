using ISO11820.UI.Tests.Infrastructure;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ISO11820.UI.Tests;

/// <summary>
/// 测试基类 —— 提供应用启动/关闭和截图能力
/// </summary>
public abstract class UITestBase : IDisposable
{
    protected AppLauncher Launcher { get; }
    protected string TestName { get; }

    protected UITestBase()
    {
        TestName = GetType().Name;
        Launcher = new AppLauncher();
    }

    /// <summary>启动应用并通过信号文件登录（推荐）</summary>
    protected void StartAndLoginViaSignal()
    {
        Launcher.Start();
        Thread.Sleep(3000);
        ClickButtonBySignal("click_login");
        Thread.Sleep(3000);
    }

    /// <summary>获取主窗口（精确匹配 "仿真系统" 标题）</summary>
    protected FlaUI.Core.AutomationElements.Window? GetMainWindow()
    {
        return Launcher.FindWindow("仿真系统", TimeSpan.FromSeconds(10));
    }

    /// <summary>启动应用并登录（管理员）— 旧版 FlaUI 方式（可能不可靠）</summary>
    protected void StartAndLoginAsAdmin()
    {
        Launcher.StartAndLogin(role: "admin", password: "123456");
        ScreenshotCapture.CaptureScreen(TestName, "01_登录后主界面");
    }

    /// <summary>启动应用并登录（试验员）</summary>
    protected void StartAndLoginAsExperimenter()
    {
        Launcher.StartAndLogin(role: "experimenter", password: "123456");
    }

    /// <summary>仅启动应用（不登录）</summary>
    protected void StartOnly()
    {
        Launcher.Start();
    }

    /// <summary>截取当前步骤</summary>
    protected string Capture(string stepName)
    {
        return ScreenshotCapture.CaptureScreen(TestName, stepName);
    }

    public void Dispose()
    {
        Launcher.Dispose();
    }

    // ═══════════════════════════════════════════════════
    //  信号文件辅助方法（供所有测试类使用）
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 通过文件信号触发 app 中的按钮点击。
    /// </summary>
    protected void ClickButtonBySignal(string signalName)
    {
        var signalDir = Path.Combine(Path.GetTempPath(), "iso11820_signals");
        Directory.CreateDirectory(signalDir);
        var signalFile = Path.Combine(signalDir, $"{signalName}.signal");

        try { File.Delete(signalFile); } catch { }
        File.WriteAllText(signalFile, DateTime.Now.ToString("o"));
    }

    /// <summary>
    /// 新建试验 — 使用信号文件触发 + Win32 操作对话框。
    /// </summary>
    protected void CreateNewTest(FlaUI.Core.AutomationElements.Window window, string productId, string testId)
    {
        ClickButtonBySignal("click_newtest");

        // 等待对话框出现
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

        // 枚举 Edit 子控件
        var editHwnds = new List<nint>();
        EnumChildWindows(dialogHwnd, (hwnd, _) =>
        {
            var cls = GetWindowClassName(hwnd);
            if (cls.StartsWith("WindowsForms10.Edit.", StringComparison.OrdinalIgnoreCase))
                editHwnds.Add(hwnd);
            return true;
        }, nint.Zero);

        if (editHwnds.Count < 10)
            throw new InvalidOperationException($"Edit 控件不足（期望≥10，实际{editHwnds.Count}）");

        // Edit 索引: 2=样品编号, 3=试验标识, 4=样品名称, 10=试验前质量
        SetTextViaWin32(editHwnds[2], productId);
        SetTextViaWin32(editHwnds[3], testId);
        SetTextViaWin32(editHwnds[4], "测试样品");
        SetTextViaWin32(editHwnds[10], "500.0");

        // 查找并点击 "创建试验" 按钮
        nint okHwnd = nint.Zero;
        EnumChildWindows(dialogHwnd, (hwnd, _) =>
        {
            if (GetWindowClassName(hwnd).Contains("Button"))
            {
                var text = GetWindowTitle(hwnd);
                if (text.Contains("创建") || text.Contains("确定") || text.Contains("OK"))
                    okHwnd = hwnd;
            }
            return true;
        }, nint.Zero);

        if (okHwnd != nint.Zero)
        {
            SendMessage(okHwnd, 0x00F5 /*BM_CLICK*/, 0, 0);
            Thread.Sleep(500);
        }
        else
        {
            // 尝试按 Enter
            SendMessage(dialogHwnd, 0x0100 /*WM_KEYDOWN*/, 0x0D /*VK_RETURN*/, 0);
            SendMessage(dialogHwnd, 0x0101 /*WM_KEYUP*/, 0x0D /*VK_RETURN*/, 0);
            Thread.Sleep(500);
        }

        Thread.Sleep(500);
    }

    // ═══════════════════════════════════════════════════
    //  Win32 P/Invoke
    // ═══════════════════════════════════════════════════

    protected delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    protected static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    protected static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    protected static extern nint FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    protected static extern bool EnumChildWindows(nint hWndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    protected static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    protected static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    protected static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    protected static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    protected static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

    protected static string GetWindowClassName(nint hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, 256);
        return sb.ToString();
    }

    protected static string GetWindowTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd) + 1;
        if (length <= 1) return "";
        var sb = new StringBuilder(length);
        GetWindowText(hwnd, sb, length);
        return sb.ToString();
    }

    protected void SetTextViaWin32(nint editHwnd, string text)
    {
        nint ptr = Marshal.StringToHGlobalAuto(text);
        try { SendMessage(editHwnd, 0x000C /*WM_SETTEXT*/, 0, ptr); }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
