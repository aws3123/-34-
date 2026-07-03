本仓库采用 Serilog 作为统一日志框架，通过全局静态 Log 实例输出结构化日志，所有日志写入本地文件。

**系统与初始化**
- 核心配置位于 src/ISO11820.App/Config/AppLogger.cs，提供 Configure() 与 CloseAndFlush() 两个入口。
- 应用启动时由 Bootstrapper.Create() 调用 AppLogger.Configure() 完成初始化，并在 Program.Main 的 finally 块中调用 CloseAndFlush() 确保退出时刷新。

**输出目标与轮转策略**
- Sink：仅使用 File Sink，日志目录为程序运行目录下的 Logs 子目录。
- 文件名模板：iso11820-.log，按天滚动（RollingInterval.Day）。
- 单文件大小限制：10 MB，超过后自动轮转；最多保留 30 个历史文件。

**日志级别与格式**
- 最小级别：Information（即忽略 Debug 及以下）。
- 输出模板：{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}，包含时间戳、级别、消息和异常信息。
- 当前代码中仅使用 Log.Information 记录关键流程事件（系统启动、配置加载、数据库初始化等），尚未发现 Warning/Error/Fatal 的使用。

**结构化字段**
- 通过 Serilog 属性占位符 {Db}、{Fs} 等形式在消息中嵌入结构化数据，便于后续查询与分析。

**其他散落的调试输出**
- UI/Chart/TemperatureChartPanel.cs 中存在多处直接 File.AppendAllText 写入临时文件 iso11820_chart_debug.txt 的调试代码，未走 Serilog 管线，属于遗留的临时调试手段。

**开发者约定**
- 新增日志应通过 Serilog.Log 静态实例，优先使用 Information 及以上级别。
- 需要携带上下文数据时使用属性占位符 {Name} 而非字符串拼接。
- 避免再使用 System.IO.File 直接写日志文件，统一走 AppLogger 管线。