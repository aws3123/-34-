# ISO 11820 UI 自动化验收测试

基于 **FlaUI**（Windows UI Automation API）的桌面应用自动化验收测试套件。

## 技术栈

| 组件 | 版本 | 用途 |
|------|------|------|
| **FlaUI.Core** | 4.0 | Windows 桌面 UI 自动化框架 |
| **FlaUI.UIA3** | 4.0 | UI Automation 3 实现 |
| **xUnit** | 2.5.3 | 测试框架 |
| **.NET** | 8.0 | 目标框架 |

## 项目结构

```
tests/ISO11820.UI.Tests/
├── ISO11820.UI.Tests.csproj       # 项目文件
├── UITestBase.cs                   # 测试基类（启动/关闭应用）
├── RunTests.ps1                    # PowerShell 运行脚本
├── Infrastructure/
│   ├── AppLauncher.cs              # 被测应用启动器
│   ├── FlaUIHelper.cs              # FlaUI 操作封装
│   ├── ScreenshotCapture.cs        # 截图工具
│   └── WaitStrategies.cs           # 等待策略
├── Tests/
│   ├── TC01_Login.cs               # 登录功能验收（9 项）
│   ├── TC02_MainLayout.cs          # 主界面布局验收（10 项）
│   ├── TC03_NewTest.cs             # 新建试验验收（5 项）
│   ├── TC04_StateMachine.cs        # 状态机流转验收（7 项）
│   ├── TC05_ButtonState.cs         # 按钮状态矩阵验收（5 项）
│   ├── TC06_Simulation.cs          # 仿真温度引擎验收（5 项）
│   ├── TC07_Messages.cs            # 系统消息日志验收（7 项）
│   ├── TC08_TestRecord.cs          # 试验现象记录验收（5 项）
│   ├── TC09_Export.cs              # 数据导出验收（5 项）
│   └── TC10_FullFlow.cs            # 端到端完整流程（1 项）
└── Screenshots/                    # 测试截图输出目录
```

## 测试清单覆盖

| 测试类 | 验收清单部分 | 测试数量 |
|--------|-------------|---------|
| TC01_Login | 第三部分：登录功能 | 9 |
| TC02_MainLayout | 第四部分：主界面布局 | 10 |
| TC03_NewTest | 第六部分：新建试验 | 5 |
| TC04_StateMachine | 第八部分：状态机流转 | 7 |
| TC05_ButtonState | 第九部分：按钮状态控制 | 5 |
| TC06_Simulation | 第七部分：仿真温度引擎 | 5 |
| TC07_Messages | 第五部分：系统消息日志 | 7 |
| TC08_TestRecord | 第十一部分：试验现象记录 | 5 |
| TC09_Export | 第十二部分：数据导出 | 5 |
| TC10_FullFlow | 第十部分：端到端完整流程 | 1 |
| **合计** | | **57** |

## 快速开始

### 前置条件

1. 确保主程序已编译：
   ```powershell
   cd C:\Users\17624\OneDrive\Desktop\jinengshijain
   dotnet build src\ISO11820.App\ISO11820.App.csproj
   ```

2. 确保测试项目已编译：
   ```powershell
   dotnet build tests\ISO11820.UI.Tests\ISO11820.UI.Tests.csproj
   ```

### 运行全部测试

```powershell
cd tests\ISO11820.UI.Tests
.\RunTests.ps1
```

### 运行特定测试

```powershell
# 仅运行登录测试
.\RunTests.ps1 -Filter "TC01"

# 仅运行端到端流程
.\RunTests.ps1 -Filter "TC10"

# 仅运行状态机测试
.\RunTests.ps1 -Filter "TC04"
```

### 列出所有测试

```powershell
.\RunTests.ps1 -ListTests
```

### 使用 dotnet test 直接运行

```powershell
# 运行全部
dotnet test tests\ISO11820.UI.Tests\ISO11820.UI.Tests.csproj

# 运行特定测试
dotnet test --filter "FullyQualifiedName~TC01"

# 查看详细输出
dotnet test --logger "console;verbosity=detailed"
```

## 测试截图

每个测试步骤都会自动截图，保存到：
```
tests/ISO11820.UI.Tests/bin/Debug/net8.0-windows/Screenshots/
├── TC01_Login/
│   ├── 01_登录后主界面.png
│   ├── 3.1.1_登录界面_角色选择.png
│   └── ...
├── TC02_MainLayout/
│   └── ...
└── TC10_FullFlow/
    └── E2E_步骤1_启动程序.png
    └── ...
```

## 关键设计

### 显式等待策略

所有等待操作使用 `WaitStrategies` 而非 `Thread.Sleep`（仿真时间除外）：

```csharp
// 等待状态文本包含关键词
WaitStrategies.WaitForLabelText(window, "_stateLabel", "就绪", TimeSpan.FromSeconds(30));

// 等待温度达到范围
WaitStrategies.WaitForTemperature(window, "_furnace1Value", 745, 755, TimeSpan.FromSeconds(30));

// 等待消息出现
WaitStrategies.WaitForMessage(window, "稳定", TimeSpan.FromSeconds(10));
```

### 控件定位

优先使用 AutomationId，其次使用 Name：

```csharp
// 推荐：通过 AutomationId
var element = window.FindFirstDescendant(cf => cf.ByAutomationId("_furnace1Value"));

// 备选：通过 Name（按钮文本）
var btn = window.FindFirstDescendant(cf =>
    cf.ByControlType(ControlType.Button).And(cf.ByName("开始升温")));
```

### 截图时机

每个测试步骤完成后自动截图：

```csharp
Step("步骤1_启动程序", () =>
{
    StartOnly();
    Thread.Sleep(2000);
});
// 自动截图保存为 E2E_步骤1_启动程序.png
```

## 已知限制

1. **仿真时间**：温度相关测试需要等待真实仿真时间（升温约 1-2 秒）
2. **窗口焦点**：测试运行时不要移动或遮挡被测应用窗口
3. **并行限制**：测试之间不并行，因为共享同一个应用实例
4. **FlaUI v4 API**：部分控件属性（如 RadioButton 选中状态）需要间接判断

## 故障排除

### 应用启动失败

```
未找到 ISO11820.App.exe
```

确保已编译主程序：
```powershell
dotnet build src\ISO11820.App\ISO11820.App.csproj
```

### 控件未找到

```
未找到按钮: 开始升温
```

- 检查应用是否正确启动并显示主界面
- 检查控件名称是否与源码一致
- 查看截图确认界面状态

### 超时

```
等待温度稳定超时
```

- 检查 appsettings.json 中的仿真参数
- 确认升温速率（HeatingRatePerSecond）和目标温度（TargetFurnaceTemp）

## 扩展测试

添加新测试：

1. 在 `Tests/` 目录创建新文件 `TC11_YourTest.cs`
2. 继承 `UITestBase`：
   ```csharp
   public class TC11_YourTest : UITestBase
   {
       [Fact]
       public void YourTestCase()
       {
           StartAndLoginAsAdmin();
           var window = Launcher.MainWindow;
           // 你的测试代码...
           Capture("步骤名称");
       }
   }
   ```

3. 编译并运行：
   ```powershell
   dotnet test --filter "TC11"
   ```

## 参考

- [FlaUI 文档](https://github.com/FlaUI/FlaUI)
- [ISO11820 验收清单](../../ISO11820-验收清单.md)
- [ISO11820 开发文档](../../ISO11820-开发文档.md)
