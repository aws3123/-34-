using Serilog;

namespace ISO11820.App.Config;

/// <summary>
/// 全局日志初始化 — 使用 Serilog + File Sink
/// </summary>
public static class AppLogger
{
    public static void Configure()
    {
        var logDirectory = Path.Combine(System.AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "iso11820-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10_485_760, // 10 MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
