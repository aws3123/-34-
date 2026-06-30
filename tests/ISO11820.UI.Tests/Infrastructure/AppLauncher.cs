using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace ISO11820.UI.Tests.Infrastructure;

/// <summary>
/// 被测应用启动器 —— 负责启动/关闭 ISO11820 桌面程序
/// </summary>
public class AppLauncher : IDisposable
{
    private Application? _application;
    private UIA3Automation? _automation;
    private readonly string _exePath;
    private readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(20);

    public AppLauncher(string? exePath = null)
    {
        _exePath = exePath ?? FindAppExe();
    }

    public Application Application => _application
        ?? throw new InvalidOperationException("应用未启动，请先调用 Start()");

    public UIA3Automation Automation => _automation
        ?? throw new InvalidOperationException("应用未启动，请先调用 Start()");

    /// <summary>
    /// 获取主窗口 —— 在所有桌面窗口中查找
    /// </summary>
    public Window MainWindow
    {
        get
        {
            // 先尝试从 Application 获取
            try
            {
                var w = _application?.GetMainWindow(_automation!, TimeSpan.FromSeconds(3));
                if (w != null) return w;
            }
            catch { }

            // 降级：从桌面查找
            var window = FindWindow("ISO 11820", _startupTimeout);
            return window ?? throw new InvalidOperationException("未找到主窗口");
        }
    }

    /// <summary>启动应用</summary>
    public void Start()
    {
        if (_application != null) return;

        // 先清理可能存在的僵尸进程
        CleanupZombieProcesses();

        _automation = new UIA3Automation();

        // 使用 Application.Launch(string) —— 避免 Attach 的 NullReferenceException
        _application = Application.Launch(_exePath);

        // 等待窗口出现（可能是登录窗口或主窗口）
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));

        // 等待至少一个 ISO11820 窗口出现
        var found = false;
        var deadline = DateTime.UtcNow + _startupTimeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var desktop = _automation.GetDesktop();
                var children = desktop.FindAllChildren();
                foreach (var child in children)
                {
                    var name = child.Properties.Name.ValueOrDefault;
                    if (name != null && name.Contains("ISO 11820"))
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            catch { }
            Thread.Sleep(500);
        }

        if (!found)
            throw new InvalidOperationException($"应用启动超时（{_startupTimeout.TotalSeconds}秒）");
    }

    /// <summary>启动应用并处理登录</summary>
    public Window StartAndLogin(string role = "admin", string password = "123456")
    {
        Start();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));

        // 查找登录窗口
        var loginWindow = FindWindow("登录", timeout: TimeSpan.FromSeconds(5));
        if (loginWindow != null)
        {
            // 选择角色
            if (role == "experimenter")
            {
                var expRadio = FindDescendantByName(loginWindow, "试验员");
                expRadio?.Click();
                Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(300));
            }

            // 输入密码 - 查找 Edit 控件
            var pwdBox = FindDescendantByType(loginWindow, ControlType.Edit);
            if (pwdBox != null)
            {
                var textBox = pwdBox.AsTextBox();
                textBox.Text = password;
            }

            // 点击登录
            var loginBtn = FindDescendantByName(loginWindow, "登录");
            loginBtn?.Click();

            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(3));
        }

        return MainWindow;
    }

    /// <summary>在桌面查找窗口（按名称模糊匹配）</summary>
    public Window? FindWindow(string titlePart, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var desktop = _automation!.GetDesktop();
                var children = desktop.FindAllChildren(cf =>
                    cf.ByControlType(ControlType.Window));
                foreach (var child in children)
                {
                    var name = child.Properties.Name.ValueOrDefault;
                    if (name != null && name.Contains(titlePart))
                        return child.AsWindow();
                }
            }
            catch { }
            Thread.Sleep(300);
        }
        return null;
    }

    /// <summary>按名称查找子元素</summary>
    public static AutomationElement? FindDescendantByName(AutomationElement parent, string name)
    {
        return parent.FindFirstDescendant(cf => cf.ByName(name));
    }

    /// <summary>按控件类型查找第一个子元素</summary>
    public static AutomationElement? FindDescendantByType(AutomationElement parent, ControlType type)
    {
        return parent.FindFirstDescendant(cf => cf.ByControlType(type));
    }

    /// <summary>关闭应用</summary>
    public void Stop()
    {
        try
        {
            _application?.Close();
        }
        catch { }

        try
        {
            _automation?.Dispose();
        }
        catch { }

        CleanupZombieProcesses();

        _automation = null;
        _application = null;
    }

    public void Dispose() => Stop();

    /// <summary>清理僵尸进程</summary>
    private static void CleanupZombieProcesses()
    {
        try
        {
            var procs = Process.GetProcessesByName("ISO11820.App");
            foreach (var p in procs)
            {
                try { p.Kill(); p.WaitForExit(2000); } catch { }
                p.Dispose();
            }
        }
        catch { }
        Thread.Sleep(500); // 等待进程完全退出
    }

    private static string FindAppExe()
    {
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionDir = FindSolutionDir(testDir);
        if (solutionDir == null)
            throw new InvalidOperationException("未找到解决方案根目录");

        var candidates = new[]
        {
            Path.Combine(solutionDir, "src", "ISO11820.App", "bin", "Debug", "net8.0-windows", "ISO11820.App.exe"),
            Path.Combine(solutionDir, "src", "ISO11820.App", "bin", "Release", "net8.0-windows", "ISO11820.App.exe"),
        };

        foreach (var c in candidates)
            if (File.Exists(c)) return c;

        throw new FileNotFoundException(
            $"未找到 ISO11820.App.exe，请先执行 `dotnet build` 编译项目。\n" +
            $"已搜索路径: {string.Join("\n", candidates)}");
    }

    private static string? FindSolutionDir(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
