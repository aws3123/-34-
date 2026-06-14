# userswdrs 任务计划书

## Git 操作要求

- **起始分支**：不要直接在 `master` 或未来统一的 `main` 上开发；先从当前集成基线拉出个人分支。如果集成负责人调整默认集成分支，按最新口径同步。
- **推荐分支名**：`feature/persistence-userswdrs`
- **提交范围**：只提交 `Config/**`、`Infrastructure/Persistence/**`、`Features/History/**`、`Features/Calibration/**` 以及对应测试，不要顺手改 UI 或 Runtime 逻辑。
- **数据库相关规则**：涉及建表 SQL、种子数据、配置路径的改动，尽量成组提交；不要把“建表修改”和“无关代码格式化”混在同一个 commit 里。
- **公共文件规则**：若必须修改 `appsettings.json`、`Shared/**` 或公共接线文件，单独 commit，并在提交说明中注明对其他模块的影响。
- **提交前验证**：提交前至少运行一次 Persistence 相关测试和 solution build；若改了初始化逻辑，再补一次真实启动验证。
- **推送与合并**：推送前确认没有把本地数据库文件、临时导入文件或测试产物带进提交。

## 任务定位
负责**持久化、历史查询、设备校准**三块，目标是把当前只有目录与占位类的基础设施层推进到“能建库、能种子初始化、能查历史、能存校准记录”的阶段。

你的重点是**SQLite 结构落地、初始化、查询入口、配置使用**，不是 UI 布局，也不是仿真状态机。

## 允许操作的文件范围
只在以下范围内修改或新增文件：

- `src/ISO11820.App/Config/**`
- `src/ISO11820.App/appsettings.json`
- `src/ISO11820.App/Infrastructure/Persistence/**`
- `src/ISO11820.App/Features/History/HistoryCoordinator.cs`
- `src/ISO11820.App/Features/Calibration/CalibrationCoordinator.cs`
- `tests/ISO11820.Tests/Persistence/**`

如确实需要新增持久化内部模型，优先放在：

- `src/ISO11820.App/Infrastructure/Persistence/Models/**`

不要把数据库结构模型塞进 `UI` 或运行时广播模型里。

## 尽量不要修改的文件
以下区域不要直接动：

- `src/ISO11820.App/UI/**`
- `src/ISO11820.App/Runtime/**`
- `src/ISO11820.App/Shared/**`
- `src/ISO11820.App/Features/Auth/**`
- `src/ISO11820.App/Features/TestExecution/**`
- `src/ISO11820.App/Features/TestRecord/**`
- `src/ISO11820.App/Features/Export/**`
- `src/ISO11820.Core/Models/TemperatureSnapshot.cs`
- `src/ISO11820.Core/Enums/TestState.cs`

## 任务拆分

### 1. 数据库初始化
按规格落地 SQLite 建表与首次初始化：

- `operators`
- `apparatus`
- `productmaster`
- `testmaster`
- `sensors`
- `CalibrationRecords`

### 2. 种子数据
确保至少能初始化：

- `admin / 123456`
- `experimenter / 123456`
- 默认设备记录
- 默认传感器记录

### 3. 历史查询入口
给 `HistoryCoordinator` 提供查询主入口，支撑后续按日期、样品编号、操作员查询。

### 4. 校准记录入口
给 `CalibrationCoordinator` 提供保存/查询入口，尤其是 `CalibrationRecords` 的 JSON 字段和表名大小写要与规格一致。

### 5. 配置接线
让数据库路径、基础目录等配置从 `appsettings.json` 读取，不要把路径硬编码到调用方。

## 完成标准
满足以下条件才算完成：

- 应用启动时能在配置路径下创建数据库文件
- 六张表创建成功，表名/字段名与规格一致
- 种子数据可以重复启动而不重复脏插入
- 历史查询与校准记录已有明确入口类，不再靠窗体直接访问数据库
- `CalibrationRecords` 维持大写表名
- 登录查询逻辑按 `username + pwd` 预留，而不是 `userid + pwd`

## 验证方式

### 自动验证
运行：

```bash
"/c/Program Files/dotnet/dotnet.exe" test "tests/ISO11820.Tests/ISO11820.Tests.csproj" --filter FullyQualifiedName~Persistence
"/c/Program Files/dotnet/dotnet.exe" build "ISO11820.sln"
```

### 文件验证
启动应用后检查：

- `appsettings.json` 指定的数据库文件已经创建
- 数据库目录自动生成成功

### 数据验证
用 SQLite 工具或测试代码确认：

- 六张表都存在
- `operators` 里有 `admin` 和 `experimenter`
- `apparatus`、`sensors` 默认数据存在
- `CalibrationRecords` 可正常插入/读取

### 代码检查
人工确认：

- `UI` 层没有写 SQL
- SQL 语句与初始化逻辑集中在 `Infrastructure/Persistence/**`
- 数据库路径从配置读取，不是硬编码

## 交付物
- 可重复执行的数据库初始化代码
- 初始种子数据写入逻辑
- 历史查询协调入口
- 校准记录协调入口
- `tests/ISO11820.Tests/Persistence/**` 下的配套测试
