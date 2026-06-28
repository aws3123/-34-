# Y-Yunye 任务计划书

## Git 操作要求

- **起始分支**：不要直接在 `master` 或未来统一的 `main` 上开发；先从当前集成基线拉出个人分支。如果后续统一切换到新的集成分支，以集成负责人通知为准。
- **推荐分支名**：`feature/export-y-yunye`
- **提交范围**：只提交 `Infrastructure/FileStorage/**`、`Features/TestRecord/**`、`Features/Export/**`、`UI/Dialogs/TestRecordDialog.cs` 和你负责的测试，不要混入 Runtime 主逻辑或主界面布局改动。
- **文件产物规则**：不要把真实导出的 CSV/Excel/PDF 文件提交进仓库；只提交代码、测试和必要的样例约定。
- **公共文件规则**：若必须调整共享模型或公共接线文件，单独 commit，并提前说明为什么现有接口不够用。
- **提交前验证**：提交前至少运行一次相关 Features/Persistence 测试和 solution build；若改了输出路径，再做一次手工路径检查。
- **提交粒度**：推荐按“记录弹窗 / 保存闭环 / CSV 输出 / 导出入口”分批提交，便于集成和回退。
- **推送与合并**：先推个人分支，待与持久化 owner 联调通过后再申请合并。

## 任务定位
负责**试验记录保存、CSV 输出、导出链路**，把“试验完成后的落库与产物输出”这条链路打通。

你的重点是：
- 完成后记录输入
- 保存后状态闭环
- CSV 路径/文件输出
- Excel/PDF 导出入口准备

不要把主要精力放到状态机或主界面布局上。

## 允许操作的文件范围
只在以下范围内修改或新增文件：

- `src/ISO11820.App/Infrastructure/FileStorage/**`
- `src/ISO11820.App/Features/TestRecord/TestRecordCoordinator.cs`
- `src/ISO11820.App/Features/Export/ExportCoordinator.cs`
- `src/ISO11820.App/UI/Dialogs/TestRecordDialog.cs`
- `tests/ISO11820.Tests/Persistence/**`
- `tests/ISO11820.Tests/Features/**`

如确实需要新增记录/导出专用模型，优先新增到：

- `src/ISO11820.App/Shared/Models/Records/**`
- `src/ISO11820.App/Shared/Models/Exports/**`

新增时不要改现有 `RuntimeSnapshot.cs` 结构。

## 尽量不要修改的文件
以下区域不要直接动：

- `src/ISO11820.App/UI/Forms/**`
- `src/ISO11820.App/Runtime/**`
- `src/ISO11820.App/Infrastructure/Persistence/**`
- `src/ISO11820.App/Features/History/**`
- `src/ISO11820.App/Features/Calibration/**`
- `src/ISO11820.Core/Enums/TestState.cs`
- `src/ISO11820.Core/Models/TemperatureSnapshot.cs`

如果保存完成后需要推动状态变化，请先和运行时负责人对齐接口，不要直接把状态机逻辑写进记录对话框。

## 任务拆分

### 1. 试验记录弹窗
在 `TestRecordDialog` 中承接：

- 试验现象
- 试验后质量
- 保存动作

### 2. 保存闭环
在 `TestRecordCoordinator` 中统一组织：

- 接收完成后的记录输入
- 调用持久化层完成保存
- 成功后把 `flag` 置到已保存状态
- 避免“已完成未保存”的数据被覆盖

### 3. CSV 文件输出
补齐 `CsvSampleWriter`，确保路径与规格一致：

- `{BaseDirectory}/TestData/{productid}/{testid}/sensor_data.csv`

至少要能稳定生成目录和目标文件名。

### 4. 导出入口
在 `ExportCoordinator` 中预留 Excel/PDF 导出主入口，让后续接 EPPlus / PDFsharp 时不需要改 UI 结构。

### 5. 测试补充
重点补：

- CSV 路径生成测试
- 记录保存参数完整性测试
- 已完成未保存阻塞规则的配套测试（如果接口准备好了）

## 完成标准
满足以下条件才算完成：

- `TestRecordDialog` 已具备输入与保存交互骨架
- `TestRecordCoordinator` 成为保存闭环入口，而不是窗体直接写库/写文件
- `CsvSampleWriter` 能稳定生成规格要求的目录和文件路径
- 导出功能已有独立协调入口
- 没有把 Excel/PDF/CSV 路径逻辑写进窗体代码里

## 验证方式

### 自动验证
运行：

```bash
"/c/Program Files/dotnet/dotnet.exe" test "tests/ISO11820.Tests/ISO11820.Tests.csproj" --filter FullyQualifiedName~Persistence
"/c/Program Files/dotnet/dotnet.exe" test "tests/ISO11820.Tests/ISO11820.Tests.csproj" --filter FullyQualifiedName~Features
"/c/Program Files/dotnet/dotnet.exe" build "ISO11820.sln"
```

### 文件验证
人工检查：

- CSV 输出目录是否符合 `productid/testid` 两级路径
- `sensor_data.csv` 文件名是否固定正确

### 流程验证
和持久化负责人联调时确认：

- 完成试验后能打开记录弹窗
- 保存成功后不会再被视为“未保存完成态”
- 导出入口可以拿到试验标识与输出目录

### 代码检查
人工确认：

- `TestRecordDialog.cs` 中没有 SQL
- `ExportCoordinator.cs` 没有依赖 UI 控件
- `CsvSampleWriter.cs` 不依赖窗体对象

## 交付物
- 试验记录保存入口
- CSV 输出路径与目录生成逻辑
- 导出协调入口
- 相关测试与联调说明
