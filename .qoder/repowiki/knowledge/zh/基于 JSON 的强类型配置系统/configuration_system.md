本仓库采用自实现的强类型 JSON 配置系统，位于 src/ISO11820.App/Config 命名空间下，核心由一组 POCO 类与静态加载器组成，不依赖 Microsoft.Extensions.Configuration。

系统与工具：
- 配置文件：appsettings.json（应用根目录），使用 System.Text.Json 反序列化。
- 日志配置：通过 AppLogger.Configure() 在启动时硬编码初始化 Serilog File Sink，未从外部文件读取。
- 无环境变量、命令行参数或 .env 支持；所有运行时开关均通过 JSON 提供。

关键文件与职责：
- Config/AppSettings.cs：定义顶层 AppSettings 及子配置段 DatabaseSettings、SimulationSettings、OutputSettings、FileStorageSettings、ReportSettings、HardwareSettings；每个子段实现 ResolvePaths(baseDirectory) 将相对路径解析为绝对路径。
- Config/AppSettingsLoader：LoadDefault() 从 BaseDirectory/appsettings.json 读取并反序列化为 AppSettings，缺失文件时返回带默认值的实例。
- Config/AppSettingsPathResolver：统一处理相对/绝对路径拼接。
- Config/AppLogger.cs：集中配置 Serilog 输出到 Logs/iso11820-.log，按日滚动、单文件 10MB、保留 30 天。
- App/Bootstrapper.cs：应用启动入口，先调用 AppLogger.Configure()，再 AppSettingsLoader.LoadDefault()，随后用 settings 构造各服务并注入到 Iso11820AppContext。
- tests/ISO11820.Tests/Features/AppSettingsLoaderTests.cs：验证默认值、相对路径合并与绝对路径透传行为。

架构与约定：
- 配置模型以 sealed class + init-only 属性表达，不可变且线程安全。
- 路径字段一律走 ResolvePaths(baseDirectory)，确保部署后路径始终相对于可执行目录。
- 配置加载顺序固定：Serilog → appsettings.json → 数据库/文件存储初始化，全部集中在 Bootstrapper.Create()。
- Iso11820AppContext 作为全局上下文持有已解析的 AppSettings，供各 Feature Coordinator 直接消费。

开发者应遵循的规则：
1. 新增配置项应在 AppSettings 下新增对应子类并实现 ResolvePaths（若涉及路径）。
2. 所有路径字段必须通过 AppSettingsPathResolver.Resolve 解析，禁止业务代码自行拼接。
3. 不要引入环境变量或命令行覆盖；如需多环境，复制 appsettings.json 并在部署时替换。
4. 修改默认值需同步更新单元测试 AppSettingsLoaderTests。
5. 日志级别、输出路径等基础设施配置集中在 AppLogger，不在 JSON 中暴露。