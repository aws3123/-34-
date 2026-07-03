本仓库为基于 WinForms 的 ISO11820 不燃性实验自动化测控系统，错误处理采用分层混合策略：UI 层通过 try/catch 捕获并 MessageBox 展示异常；业务协调器（Coordinator）使用返回 ExportResult 的 Result 模式表达成功/失败；参数校验统一使用 .NET 内置 ArgumentNullException.ThrowIfNull / ArgumentException.ThrowIfNullOrWhiteSpace；全局异常通过 Program.Main 的 try/finally 包裹 Bootstrapper 与 Application.Run，并由 Serilog 文件日志兜底。

## 1. 使用的框架与工具
- Serilog：全局日志配置在 AppLogger.Configure()，输出到 Logs/iso11820-.log，按日滚动、单文件 10MB、保留 30 份，模板包含 {Exception}。
- WinForms MessageBox：UI 层直接调用 MessageBox.Show(...) 向用户提示错误或信息。
- .NET 内置断言式参数校验：广泛使用 ArgumentNullException.ThrowIfNull、ArgumentException.ThrowIfNullOrWhiteSpace 快速失败。
- Result 模式：导出模块定义 ExportResult record，封装 Success/FilePath/Error/Message，避免抛出异常。

## 2. 关键文件与位置
- src/ISO11820.App/Program.cs — 应用入口，try/finally 包裹整个 UI 生命周期，确保日志 flush。
- src/ISO11820.App/Config/AppLogger.cs — Serilog 初始化与关闭。
- src/ISO11820.App/Features/Export/ExportCoordinator.cs — Result 模式的典型实现，所有导出方法 catch 异常后返回 ExportResult.Failed。
- src/ISO11820.Core/Models/SystemMessage.cs — 运行时消息记录模型（非错误类型，仅用于 UI 消息列表）。
- src/ISO11820.App/UI/Forms/MainForm.cs — UI 层多处 try/catch 捕获异常并显示 MessageBox。

## 3. 架构与约定
- 参数校验层：立即抛 ArgumentNullException / ArgumentException，无自定义异常类。
- 业务协调层（Coordinator）：返回 ExportResult（Result 模式），对可预期失败场景不抛异常。
- UI 层：try/catch -> MessageBox.Show，捕获上层抛出的异常或 Result.Error，以对话框形式反馈用户。
- 全局兜底：Program.Main try/finally + Serilog，未捕获异常沿 WinForms 事件链向上，Serilog 负责写入堆栈。

设计决策：
- 没有定义统一的自定义异常类，复用 .NET 标准异常。
- 对“可恢复的业务失败”使用 Result 模式；对“编程错误”直接抛异常。
- 日志只作为诊断手段，不替代返回值中的错误信息。

## 4. 开发者应遵循的规则
1. 参数校验优先：在公共 API 入口使用 ThrowIfNull / ThrowIfNullOrWhiteSpace 快速失败，不要吞掉异常。
2. 可预期失败用 Result：涉及 I/O、外部依赖的方法，返回 ExportResult 而非抛异常，让调用方显式处理成功/失败分支。
3. UI 层只负责呈现：不要在 UI 中自行构造详细错误文本，统一通过 MessageBox.Show 展示；复杂错误信息先记录到 Serilog。
4. 全局日志必须 flush：任何新增的全局异常处理器都应在 finally 中调用 AppLogger.CloseAndFlush()，参考 Program.Main。
5. 不要滥用 catch { }：现有代码中存在少量空 catch（如临时文件删除），仅限资源清理场景且不影响主流程。