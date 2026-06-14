# Nlpexi-e 任务计划书

## Git 操作要求

- **起始分支**：不要直接在 `master` 或未来统一的 `main` 上开发；先从当前集成基线拉出个人分支。如果后续仓库统一切换集成分支，以集成负责人通知为准。
- **推荐分支名**：`feature/ui-nlpexi-e`
- **提交范围**：只提交 `UI/**`、`Features/Auth/**`、`Features/TestExecution/**` 以及你负责的轻量测试，不要混入 Runtime 或 Persistence 的实现。
- **公共文件规则**：若确实需要改 `Program.cs`、`Shared/**`、`App/**` 这类共享入口文件，必须单独 commit，并提前和骨架 owner 沟通。
- **提交前验证**：每次提交前至少运行一次 `dotnet build ISO11820.sln`；涉及窗体/线程切换时，再做一次本地 `run` 手动验证。
- **提交粒度**：推荐按“登录界面 / 主界面布局 / 弹窗 / 按钮状态控制”分批提交，便于回滚和评审。
- **推送与合并**：推送后以评审合并为准，不直接把 UI 改动压到公共分支。

## 任务定位
负责**主界面与交互层**，把当前可运行的窗体骨架推进为“能承载登录、新建试验、实时显示、按钮状态控制”的 UI 壳层。

你的重点是**界面结构、交互流程、线程切换、UI 绑定**，不是状态机实现，也不是 SQLite 细节。

## 允许操作的文件范围
只在以下范围内修改或新增文件：

- `src/ISO11820.App/UI/Common/**`
- `src/ISO11820.App/UI/ViewModels/**`
- `src/ISO11820.App/UI/Forms/MainForm.cs`
- `src/ISO11820.App/UI/Forms/LoginForm.cs`
- `src/ISO11820.App/UI/Dialogs/NewTestDialog.cs`
- `src/ISO11820.App/Features/Auth/AuthCoordinator.cs`
- `src/ISO11820.App/Features/TestExecution/TestExecutionCoordinator.cs`
- `tests/ISO11820.Tests/Features/**`（仅限 UI/交互相关轻量测试）

## 尽量不要修改的文件
以下文件不要直接改，避免和其他人冲突：

- `src/ISO11820.App/Runtime/**`
- `src/ISO11820.App/Infrastructure/**`
- `src/ISO11820.App/Features/History/**`
- `src/ISO11820.App/Features/Calibration/**`
- `src/ISO11820.App/Features/Export/**`
- `src/ISO11820.App/Features/TestRecord/**`
- `src/ISO11820.App/Shared/Events/DataBroadcastEventArgs.cs`
- `src/ISO11820.App/Shared/Models/RuntimeSnapshot.cs`
- `src/ISO11820.App/Program.cs`（如需改启动流程，先沟通）

## 任务拆分

### 1. 登录界面壳层
完成角色选择、密码输入、登录按钮、错误提示的界面组织；登录校验本身交给后续业务逻辑，不要把数据库校验塞进窗体代码。

### 2. 主界面布局
给 `MainForm` 留好以下区域：

- 当前状态显示
- 5 通道温度显示区域
- 图表区域占位
- 按钮组（新建试验 / 开始升温 / 停止升温 / 开始记录 / 停止记录 / 参数设置）
- 系统消息区

### 3. 新建试验弹窗
在 `NewTestDialog` 中先铺好字段区和提交动作占位，为后续接入实际保存逻辑做准备。

### 4. UI 线程切换护栏
继续保持 `DataBroadcast` 事件只通过 `Invoke`/`InvokeRequired` 回到 UI 线程更新控件，不能在后台线程直接写控件。

### 5. 按钮启停矩阵
先把按钮状态控制抽到 UI 层内部的统一位置，不要把启停逻辑散落在多个 click 事件中。

## 完成标准
满足以下条件才算完成：

- `MainForm` 已具备清晰的结构分区，不再只是一个中心标签
- `LoginForm` 与 `NewTestDialog` 已具备后续接业务的表单骨架
- `MainForm` 继续通过 `Invoke` 消费广播，未引入跨线程 UI 风险
- UI 代码中没有 SQL、没有 `Microsoft.Data.Sqlite`、没有直接写文件路径
- 按钮状态控制有统一入口，不靠零散 if/else 到处写

## 验证方式

### 自动验证
运行：

```bash
"/c/Program Files/dotnet/dotnet.exe" build "ISO11820.sln"
```

### 手动验证
在 Windows 本地运行：

```bash
"/c/Program Files/dotnet/dotnet.exe" run --project "src/ISO11820.App/ISO11820.App.csproj"
```

检查：

- 主窗体正常打开
- 没有跨线程异常
- 登录窗体/新建试验弹窗能正常打开
- 各区域布局清晰、控件命名可读

### 代码检查
人工确认：

- `MainForm.cs` 没有 SQL 代码
- `MainForm.cs` 没有状态机核心判断
- 所有 UI 更新都经过 `InvokeRequired` 检查

## 交付物
- 登录窗体骨架
- 主界面骨架与控件区域划分
- 新建试验弹窗骨架
- UI 层按钮状态控制入口
