本仓库为 WinForms 桌面应用，未使用任何 CSS/SCSS/Tailwind 等 Web 样式体系，也未引入第三方 UI 框架（如 MaterialSkin、Fluent.Ribbon、Avalonia）。前端风格完全由 .NET Framework / .NET 6+ 的 WinForms 原生控件通过代码内联属性定义，遵循以下约定：

1. **样式定义方式**
   - 所有视觉属性（字体、颜色、尺寸、边框）均在 Form/Panels/Dialogs 构造函数中以 C# 对象初始化表达式直接设置，例如 `Font = new Font("Microsoft YaHei", 10F)`、`BackColor = Color.FromArgb(235, 235, 235)`。
   - 不存在 `.Designer.cs` 或 `.resx` 资源文件驱动布局；窗体完全以程序化方式构建。
   - 未调用 `Application.EnableVisualStyles()` / `SetCompatibleTextRenderingDefault(false)`，因此控件外观依赖操作系统主题。

2. **设计令牌（硬编码色板）**
   - 背景灰阶：`Color.FromArgb(235,235,235)`（状态栏）、`Color.FromArgb(245,245,245)`（按钮面板）、`Color.FromArgb(248,248,248)`（筛选区）。
   - 深色温度面板：`BackColor = Color.FromArgb(30,30,30)`，配合亮绿数值 `ForeColor = Color.FromArgb(0,255,128)`。
   - 强调色：`DarkBlue`（温漂标签）、`DarkOrange`（终止消息）、`Red`（错误提示）。
   - 这些色值在 MainForm、LoginForm、CalibrationPanel 等处重复出现，但尚未抽取到集中配置类。

3. **字体与排版约定**
   - 统一使用 `Microsoft YaHei` 作为中文字体。
   - 标题/状态用 11F~18F Bold，正文 9F~10F，温度数值 11F。
   - 行高/间距通过固定 `Height`、`Padding`、`Margin` 像素值控制，无响应式缩放逻辑。

4. **布局策略**
   - 主界面采用 Dock 组合：Bottom → Top → Right → Left → Fill 顺序添加子 Panel，确保 z-order 正确。
   - 登录页使用 `TableLayoutPanel` + `FlowLayoutPanel` 做网格对齐。
   - 图表区域使用自定义 `TemperatureChartPanel`（继承自 `UserControl`），通过 `ExportImage` 导出位图供 Excel/PDF 报告复用。

5. **可访问性与测试钩子**
   - 为自动化测试注入文件信号监听（`iso11820_signals/*.signal`）并通过 `SendMessage` 转发到 UI 线程，不影响正常交互。
   - 未启用 Windows 视觉样式，控件外观保持经典 WinForms 渲染。

6. **开发者应遵循的规则**
   - 新增控件时沿用 `Microsoft YaHei` 字体族和现有色板常量，避免散落魔法数字。
   - 复杂布局优先使用 `Dock` + `Panel` 组合，必要时用 `TableLayoutPanel` 做表单对齐。
   - 跨线程 UI 更新必须经 `InvokeRequired` + `Invoke` 路由（参见 `OnDataBroadcast`）。
   - 如需全局主题切换，应将当前硬编码色值迁移至集中配置类（如 `AppSettings` 扩展），而非继续内联。