---
name: winforms-layout-debug
description: Use when diagnosing WinForms layout issues — controls overlapping, text truncated, controls not visible, OxyPlot PlotView not rendering until resize, or any UI occlusion/clipping problem. Covers a standard 5-step diagnostic procedure: Dock order, Z-order, rendering timing, parent container sizing, and DPI scaling. Also use when the user reports "控件遮挡", "截断", "曲线不显示", "布局错乱", "窗口缩放后才正常", or any WinForms rendering anomaly.
---

# WinForms 布局遮挡/截断问题标准排查修复流程

## 概述

WinForms 的 Dock/Anchor 布局系统容易因添加顺序、渲染时序、DPI 缩放等因素导致控件被遮挡、截断或不可见。本技能提供一套系统化的五步排查流程，按可能性从高到低排列。

## 核心原则

**先诊断，后修复。** 每一步都必须先收集实际数据（Bounds、ClientSize、Z-Order），确认问题后再动手。不要凭猜测跳步。

---

## 第 1 步：检查 Dock 顺序（最常见根因，约 60% 的问题）

### 原理

Dock 的 `Controls.Add` 顺序决定控件占用空间后的剩余空间分配。**后添加的控件先占据空间，先添加的控件被挤到剩余空间。** 正确的顺序永远是：Bottom → Top → Right → Left → Fill。

如果中间某个 Panel 的 `Height`/`Width` 设置错误，或者 Fill 控件没有显式设置 `Dock = DockStyle.Fill`，就会导致布局错乱。

### 诊断操作

在每个控件的父容器中，遍历其子控件并打印 Dock 和 Bounds：

```csharp
// 诊断代码：放在父容器 Layout 事件或 Shown 事件中
foreach (Control c in parentPanel.Controls)
{
    Debug.WriteLine($"[Dock={c.Dock}] {c.Name}: Bounds={c.Bounds}, ClientSize={c.ClientSize}");
}
```

### 常见问题与修复

| 问题 | 现象 | 修复 |
|------|------|------|
| Dock 顺序错误 | Fill 控件被 Right/Left 控件遮挡 | 调整 `Controls.Add` 顺序：Bottom → Top → Right → Left → Fill |
| Fill 控件未设 Dock | 控件尺寸为 0 或极小 | 显式设置 `control.Dock = DockStyle.Fill` |
| Panel 的 Height/Width 写死 | 子控件被截断 | 确认 `Height`/`Width` 足够容纳所有子控件 |
| 子控件使用绝对坐标 | 在不同分辨率下偏移 | 子控件也用 Dock 或 Anchor，避免硬编码 `Location` |

### 验证方式

修复后，在 `Shown` 事件中打印每个控件的最终 `Bounds`，确认没有控件尺寸为 0×0。

---

## 第 2 步：检查 Z-Order（层叠顺序，约 20% 的问题）

### 原理

WinForms 中控件的 Z-Order（渲染层级）由 `Controls.SetChildIndex()` 决定。同一 Dock 区域内的多个控件，Z-Order 高的会遮挡 Z-Order 低的。另外，如果两个控件 Dock 值相同（比如都是 Top），后添加的会排在前一个的下方。

### 诊断操作

```csharp
// 诊断代码：打印父容器中所有子控件的 Z-Order
for (int i = 0; i < parentPanel.Controls.Count; i++)
{
    var c = parentPanel.Controls[i];
    Debug.WriteLine($"Z-Order[{i}]: {c.Name}, Dock={c.Dock}, Bounds={c.Bounds}");
}
```

### 常见问题与修复

| 问题 | 修复 |
|------|------|
| 控件被其他控件遮挡 | `parentPanel.Controls.SetChildIndex(control, 0)` 置于最底层 |
| 同 Dock 控件顺序颠倒 | 调整 `Controls.Add` 的先后顺序 |
| 动态创建的控件跑到最上层 | 创建后立即调用 `SendToBack()` 或 `SetChildIndex(control, 0)` |

---

## 第 3 步：检查第三方控件渲染时序（OxyPlot 等，约 15% 的问题）

### 原理

OxyPlot 的 `PlotView`（以及某些自绘控件）在控件 `ClientSize` 为 0×0 时不会执行渲染。WinForms 在 `Load` 事件时控件尺寸可能尚未最终确定，导致初始渲染空白。**窗口缩放后触发重新布局和重绘，控件获得正确尺寸，曲线才显示出来。**

这是 OxyPlot 的已知问题（GitHub Issue #1046），不仅限于 OxyPlot——任何依赖 `OnPaint` 或 `Invalidate` 的自绘控件都可能遇到。

### 诊断操作

在疑似有问题的控件上打印其初始尺寸：

```csharp
// 在 Shown 事件中
Debug.WriteLine($"[Shown] {plotView.Name}: Size={plotView.Size}, ClientSize={plotView.ClientSize}");
// 在 Resize 事件中
Debug.WriteLine($"[Resize] {plotView.Name}: Size={plotView.Size}, ClientSize={plotView.ClientSize}");
```

如果 `Shown` 时 `ClientSize` 为 (0, 0)，但 `Resize` 后变为正常值，则确认是渲染时序问题。

### 修复方案（按可靠性排序）

**方案 A — Shown 事件刷新（最可靠，推荐首选）：**
```csharp
Shown += (_, _) =>
{
    _view.InvalidatePlot(true);  // 强制全量重绘
    _view.Refresh();              // 触发控件刷新
};
```

**方案 B — BeginInvoke 延迟渲染（适用于控件嵌套较深时）：**
```csharp
BeginInvoke(new Action(() =>
{
    _view.Model = model;
    _view.InvalidatePlot(true);
}));
```

**方案 C — OnLayout override（每次布局变化都刷新，适用于动态布局场景）：**
```csharp
protected override void OnLayout(LayoutEventArgs e)
{
    base.OnLayout(e);
    _view.InvalidatePlot(true);
}
```

### 注意

- `InvalidatePlot(true)` 中的 `true` 参数表示强制更新数据，不可省略
- 方案 A 和 B 可以组合使用
- 如果仍然无效，检查控件的 `Visible` 属性是否为 `true`

---

## 第 4 步：检查父容器尺寸（约 4% 的问题）

### 原理

Panel 内的子控件使用**相对于 Panel 客户区的坐标**。如果 Panel 本身被其他 Dock 控件挤压到尺寸过小，子控件就会被截断——即使子控件的 `Location` 和 `Size` 在代码中看起来正确。

### 诊断操作

在 Panel 的 `Resize` 事件中打印 `ClientSize`：

```csharp
panel.Resize += (_, _) =>
    Debug.WriteLine($"[Resize] {panel.Name}: ClientSize={panel.ClientSize}, " +
                     $"子控件需要的空间={panel.Controls.Cast<Control>().Max(c => c.Bottom)}");
```

如果 `ClientSize.Height` 或 `Width` 小于子控件需要的空间，则确认是父容器尺寸问题。

### 常见问题与修复

| 问题 | 修复 |
|------|------|
| Panel 高度不够，底部控件被截断 | 增大 Panel 的 `Height`，或给 Panel 设置 `AutoScroll = true` |
| Panel 宽度不够，右侧控件被截断 | 增大 Panel 的 `Width`，或减小子控件的 `Width` |
| 嵌套 Panel 层级过深导致累积偏移 | 减少嵌套层级，或使用 `TableLayoutPanel` 替代多层嵌套 |

---

## 第 5 步：检查 DPI 缩放（约 1% 的问题，但高 DPI 屏幕下常见）

### 原理

高 DPI 屏幕（150%、200% 缩放）下，WinForms 默认的 GDI 缩放可能导致控件尺寸计算错误、字体渲染模糊、控件位置偏移。

### 诊断操作

```csharp
// 在 Program.cs 或 MainForm 构造函数中
Debug.WriteLine($"DPI: {DeviceDpi}, ScaleFactor: {DeviceDpi / 96f:P0}");
```

### 修复

在 `Program.cs` 的 `Main()` 方法中，**在任何 UI 代码之前**添加：

```csharp
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
```

同时在 `app.manifest` 中启用 DPI 感知：

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings>
</application>
```

---

## 快速排查检查清单

当用户报告布局问题时，按以下顺序逐项确认：

1. [ ] 所有 Dock=Fill 的控件是否显式设置了 `Dock = DockStyle.Fill`？
2. [ ] `Controls.Add` 的顺序是否为 Bottom → Top → Right → Left → Fill？
3. [ ] 是否有控件初始 `Bounds` 为 0×0（在 Shown 事件中打印确认）？
4. [ ] 第三方控件（如 OxyPlot PlotView）是否在 Shown 事件中调用了 `InvalidatePlot(true)` + `Refresh()`？
5. [ ] 父 Panel 的 `Height`/`Width` 是否大于所有子控件 `Bottom`/`Right` 的最大值？
6. [ ] 是否启用了 `SetHighDpiMode(HighDpiMode.PerMonitorV2)`？
7. [ ] 所有 Panel 的 `Padding` 和 `Margin` 是否考虑在内？

## 项目特定信息

本项目的 `MainForm` 使用 WinForms Dock 布局，包含 OxyPlot `TemperatureChartPanel`。已知的嫌疑点：

- `_chartPanel.View`（PlotView）需要显式设置 `Dock = DockStyle.Fill`，并在 Shown 事件中调用 `InvalidatePlot(true)`
- 右侧按钮 Panel 宽度 160px，按钮宽度 136px + 左边距 12px，刚好够用，需注意 Padding 和 Border
- 状态栏 Label 使用绝对坐标 `Location`，在高 DPI 下可能偏移