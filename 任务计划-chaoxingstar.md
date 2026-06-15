# chaoxingstar 任务计划书

> 状态：已完成（已合并到 `main`）

## Git 操作要求

- **起始分支**：不要直接在 `master` 或未来统一的 `main` 上开发；先从当前集成基线拉出个人分支。如果集成负责人后续明确把默认集成分支切到 `main`，以最新通知为准。
- **推荐分支名**：`feature/runtime-chaoxingstar`
- **提交范围**：只提交运行时 owner 负责的文件；不要顺手提交 UI、Persistence、Export 的功能改动。
- **公共文件规则**：如果必须改 `Program.cs`、`App/Bootstrapper.cs`、`App/Iso11820AppContext.cs`、`Shared/**` 这类高冲突文件，单独做一个 commit，并在说明里写清改动原因。
- **提交前验证**：提交前至少执行一次本计划书里的 Runtime 测试和 solution build，确认不会把主干接线打坏。
- **提交粒度**：推荐按“状态机 / 仿真 / 广播 / 测试”拆成多个小提交，不要把所有内容揉成一个超大 commit。
- **推送与合并**：推送后先发起评审，再合并回集成分支；未确认前不要直接改别人分支。

## 任务定位
负责**运行时引擎与状态机主轴**，把试验流程从“静态骨架”推进到“有节奏更新、有明确状态流转、能稳定向 UI 广播”的阶段。

当前主干已经完成：
- 解决方案/项目骨架已建立
- `dotnet build ISO11820.sln` 已通过
- `dotnet test tests/ISO11820.Tests/ISO11820.Tests.csproj` 已通过

你的任务重点不是做 UI，也不是做数据库，而是把**状态机、仿真、广播契约**做扎实。

## 允许操作的文件范围
只在以下范围内修改或新增文件：

- `src/ISO11820.Core/Enums/TestState.cs`
- `src/ISO11820.Core/Models/**`
- `src/ISO11820.Core/Runtime/**`
- `src/ISO11820.Core/Contracts/**`
- `src/ISO11820.App/Shared/Models/RuntimeSnapshot.cs`
- `src/ISO11820.App/Shared/Events/DataBroadcastEventArgs.cs`
- `src/ISO11820.App/Runtime/Controller/TestController.cs`
- `src/ISO11820.App/Runtime/Services/SensorSimulator.cs`
- `src/ISO11820.App/Runtime/Services/DaqWorker.cs`
- `tests/ISO11820.Tests/Runtime/**`

## 尽量不要修改的文件
以下文件属于共享接线区，除非先沟通，否则不要直接改：

- `src/ISO11820.App/Program.cs`
- `src/ISO11820.App/App/Bootstrapper.cs`
- `src/ISO11820.App/App/Iso11820AppContext.cs`
- `src/ISO11820.App/UI/**`
- `src/ISO11820.App/Infrastructure/**`
- `src/ISO11820.App/Features/History/**`
- `src/ISO11820.App/Features/Calibration/**`
- `src/ISO11820.App/Features/Export/**`
- `src/ISO11820.App/Features/TestRecord/**`

## 任务拆分

### 1. 固化状态机规则
把以下状态流转收进统一入口，不要分散在多个按钮事件中：

- `Idle -> Preparing`
- `Preparing -> Ready`
- `Ready -> Recording`
- `Recording -> Complete`
- `Ready -> Preparing`（温度不稳定时自动回退）

### 2. 补齐仿真节拍
根据规格实现以 800ms 为节拍的仿真推进逻辑：

- 升温阶段
- 稳定阶段
- 记录阶段
- 停止加热后的冷却阶段

### 3. 统一广播模型
保证运行时只向外暴露稳定的快照对象，而不是让 UI 自己拼温度、状态和消息。

### 4. 明确消息来源
把系统消息的产生点和广播时机固定下来，后续 UI 只消费消息，不负责拼消息内容。

### 5. 补测试
至少补以下类别的单元测试：

- 状态转移测试
- `Ready -> Preparing` 回退测试
- 仿真初值/阶段推进测试
- 广播快照结构测试

## 完成标准
满足以下条件才算完成：

- 状态机入口唯一，外部不用自己拼状态流转逻辑
- `SensorSimulator` 不再只是返回初始值，已经能按节拍推进数据
- `DaqWorker` 能稳定驱动广播
- `RuntimeSnapshot` 能承载 UI 所需的当前状态、温度、消息
- 相关测试已补齐并通过
- `ISO11820.Core` 中没有 `Form`、`Control`、`Invoke`、`Sqlite` 等 UI/基础设施依赖

## 验证方式

### 自动验证
运行：

```bash
"/c/Program Files/dotnet/dotnet.exe" test "tests/ISO11820.Tests/ISO11820.Tests.csproj" --filter FullyQualifiedName~Runtime
"/c/Program Files/dotnet/dotnet.exe" build "ISO11820.sln"
```

### 代码检查
人工确认：

- `src/ISO11820.Core/**` 里没有 WinForms 引用
- `TestController` 是状态流转入口，而不是 `MainForm`
- `SensorSimulator` 的推进规则与规格文档一致

### 联调检查
和 UI 负责人联调时确认：

- 程序启动后能收到首帧广播
- UI 不需要自己计算状态迁移
- 运行时广播可以驱动温度/状态显示

## 交付物
- 运行时状态机代码
- 仿真推进逻辑
- 广播契约稳定版
- `tests/ISO11820.Tests/Runtime/**` 下的配套测试
