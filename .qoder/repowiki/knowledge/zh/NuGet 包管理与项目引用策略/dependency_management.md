本仓库采用 .NET SDK 风格的 csproj 进行依赖管理，未使用 packages.config、nuget.config 或 global.json，所有第三方库通过 PackageReference 在各自项目的 .csproj 中声明版本。

1. 使用的系统与工具
- 包管理器：NuGet（由 MSBuild/NuGet CLI 驱动）
- 目标框架：net8.0 / net8.0-windows
- 构建系统：MSBuild SDK 风格项目文件（Sdk="Microsoft.NET.Sdk"）
- 无全局包源/私有源配置（仓库内未发现 nuget.config），默认使用 nuget.org

2. 关键文件与包
- 应用层 src/ISO11820.App/ISO11820.App.csproj：声明 EPPlus、MathNet.Numerics、Microsoft.Data.Sqlite、OxyPlot.WindowsForms、PDFsharp-MigraDoc、Serilog 等运行时依赖
- 核心层 src/ISO11820.Core/ISO11820.Core.csproj：零外部 NuGet 依赖，仅含内部模型与接口
- 单元测试 tests/ISO11820.Tests/ISO11820.Tests.csproj：xunit + Microsoft.NET.Test.Sdk + coverlet.collector
- UI 自动化测试 tests/ISO11820.UI.Tests/ISO11820.UI.Tests.csproj：FlaUI.Core/UIA3 + System.Drawing.Common，通过启动进程方式运行被测 WinForms 程序，不直接引用 App 项目

3. 架构与约定
- 分层依赖：Core 为纯 .NET Standard 类库，App 通过 ProjectReference 引用 Core；测试项目同样以 ProjectReference 注入被测代码，形成清晰的单向依赖链
- 版本锁定：所有 PackageReference 均显式指定版本号（如 EPPlus 7.5.2、Serilog 4.0.2），未使用浮动版本范围
- 无锁文件：仓库未提交 packages.lock.json，每次还原可能产生不同版本的二进制
- 无 vendoring：未将任何第三方 DLL 放入 vendor/ 或 lib/ 目录
- 私有源：未发现 nuget.config，意味着依赖全部来自公共 NuGet 源

4. 开发者应遵循的规则
- 新增依赖时，仅在对应 .csproj 的 ItemGroup 中添加 PackageReference 并固定具体版本号
- 保持 Core 层零外部依赖，业务相关库放在 App 层
- 测试依赖（xunit、FlaUI 等）仅出现在 tests 目录下对应项目中，避免污染生产输出
- 如需引入私有 NuGet 源，应在仓库根添加 nuget.config 并在 CI 环境中同步配置
- 建议启用 packages.lock.json 以保证可重复构建（当前未启用）