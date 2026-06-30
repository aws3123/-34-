using FlaUI.Core.Capturing;

namespace ISO11820.UI.Tests.Infrastructure;

/// <summary>
/// 截图工具 —— 在测试关键步骤自动截取窗口/屏幕画面
/// </summary>
public static class ScreenshotCapture
{
    private static readonly object _lock = new();
    private static readonly string _baseDir;

    static ScreenshotCapture()
    {
        _baseDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Screenshots");
        Directory.CreateDirectory(_baseDir);
    }

    /// <summary>截取整个屏幕</summary>
    public static string CaptureScreen(string testName, string stepName)
    {
        var dir = Path.Combine(_baseDir, Sanitize(testName));
        Directory.CreateDirectory(dir);

        var fileName = $"{DateTime.Now:HHmmss}_{Sanitize(stepName)}.png";
        var filePath = Path.Combine(dir, fileName);

        lock (_lock)
        {
            using var image = FlaUI.Core.Capturing.Capture.Screen();
            image.ToFile(filePath);
        }

        return filePath;
    }

    /// <summary>获取截图输出根目录</summary>
    public static string GetBaseDirectory() => _baseDir;

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
