本项目采用 .NET 8 SDK 与 Visual Studio Solution 作为唯一构建系统，未引入 Makefile、Dockerfile、CI/CD 流水线或自定义打包脚本。所有编译、测试、运行均通过 `dotnet` CLI 和 Visual Studio 完成。

**解决方案结构**
- 根目录 `ISO11820.sln` 定义两个顶层分组：`src`（应用代码）和 `tests`（测试），包含四个项目：
  - `src/ISO11820.Core` — 无 UI 依赖的纯 .NET 8 类库，仅输出 POCO、枚举、接口等核心模型
  - `src/ISO11820.App` — WinForms 桌面程序（`OutputType=WinExe`，`TargetFramework=net8.0-windows`），引用 Core 并承载 UI、业务协调器、持久化、采集服务等全部功能
  - `tests/ISO11820.Tests` — xUnit 单元测试，同时引用 App 与 Core，覆盖业务逻辑与持久化层
  - `tests/ISO11820.UI.Tests` — 基于 FlaUI 的 UI 自动化验收测试，不直接引用被测程序，而是通过路径启动已编译的 App 可执行文件

**目标框架与平台约束**
- Core 使用跨平台的 `net8.0`，便于在 CI 中独立编译和测试
- App 与 UI 测试锁定 `net8.0-windows`，依赖 Windows Forms 与 OxyPlot.WindowsForms，无法在非 Windows 环境运行
- 所有项目统一启用 `<Nullable>enable</Nullable>` 与 `<ImplicitUsings>enable</ImplicitUsings>`，遵循现代 C# 风格

**依赖管理**
- 通过 `.csproj` 内嵌 `<PackageReference>` 声明 NuGet 包，版本固定（如 EPPlus 7.5.2、Serilog 4.0.2、Microsoft.Data.Sqlite 8.0.0、FlaUI 4.0 等），不使用 `packages.config` 或全局包源策略
- 运行时资源（`appsettings.json`、`xunit.runner.json`）通过 `<None Update ... CopyToOutputDirectory="PreserveNewest">` 复制到输出目录

**测试与验证**
- 单元测试通过 `dotnet test` 运行，集成 coverlet.collector 收集覆盖率
- UI 自动化测试由 `tests/ISO11820.UI.Tests/RunTests.ps1` PowerShell 脚本驱动，自动执行“编译主程序 → 编译测试项目 → dotnet test”流程，支持 `-Filter` 筛选用例、`--results-directory` 输出测试结果、截图归档到 `Screenshots/` 目录
- 测试项目标记 `<IsTestProject>true</IsTestProject>` 与 `<IsPackable>false</IsPackable>`，避免被误打包

**发布与部署**
- 未发现任何发布脚本、自包含发布（`dotnet publish --self-contained`）、安装包生成或容器化配置；当前产物为单文件 WinExe，直接分发即可
- 版本信息未在 csproj 中显式设置，默认由 MSBuild 自动生成

**开发者约定**
- 新增模块应放在 `src/ISO11820.App` 下对应 Feature 目录，并通过 ProjectReference 引用 Core
- 新增测试应放入 `tests/ISO11820.Tests`（单元）或 `tests/ISO11820.UI.Tests`（UI），保持与源码目录一致的命名空间映射
- 所有构建命令以 `dotnet build` / `dotnet test` 为准，PowerShell 脚本仅封装 UI 测试的额外步骤