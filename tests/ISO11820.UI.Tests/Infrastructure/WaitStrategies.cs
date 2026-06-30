using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;

namespace ISO11820.UI.Tests.Infrastructure;

/// <summary>
/// 自定义等待策略 —— 用于仿真场景中的异步状态等待
/// WinForms 控件不暴露字段名作为 AutomationId，使用文本前缀匹配
/// </summary>
public static class WaitStrategies
{
    /// <summary>等待包含指定前缀的 Label 文本中出现关键词</summary>
    public static bool WaitForLabelText(Window window, string textPrefix, string expectedText,
        TimeSpan? timeout = null, string? description = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // 搜索所有 Text 控件，找到包含 textPrefix 的那个
                var labels = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                foreach (var label in labels)
                {
                    var name = label.Properties.Name.ValueOrDefault;
                    if (name != null && name.Contains(textPrefix) && name.Contains(expectedText))
                        return true;
                }
            }
            catch { /* 元素可能暂未就绪 */ }
            Thread.Sleep(300);
        }

        var desc = description ?? $"等待包含 '{textPrefix}' 的标签出现 '{expectedText}'";
        throw new TimeoutException($"[{desc}] 超时 ({(timeout ?? TimeSpan.FromSeconds(30)).TotalSeconds}秒)");
    }

    /// <summary>等待 Label 文本包含指定关键词（返回 bool，不抛异常）</summary>
    public static bool TryWaitForLabelText(Window window, string textPrefix, string expectedText,
        TimeSpan? timeout = null)
    {
        try
        {
            return WaitForLabelText(window, textPrefix, expectedText, timeout);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>等待按钮变为可用/不可用</summary>
    public static bool WaitForButtonState(Window window, string buttonName, bool expectedEnabled,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var btn = window.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Button).And(cf.ByName(buttonName)));
                if (btn != null)
                {
                    var isEnabled = btn.Properties.IsEnabled.Value;
                    if (isEnabled == expectedEnabled)
                        return true;
                }
            }
            catch { /* 元素可能暂未就绪 */ }
            Thread.Sleep(200);
        }

        throw new TimeoutException(
            $"等待按钮 '{buttonName}' {(expectedEnabled ? "启用" : "禁用")} 超时");
    }

    /// <summary>等待温度数值达到指定范围（通过文本前缀查找）</summary>
    public static bool WaitForTemperature(Window window, string textPrefix,
        double minValue, double maxValue, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var labels = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                foreach (var label in labels)
                {
                    var text = label.Properties.Name.ValueOrDefault;
                    if (text != null && text.Contains(textPrefix) && TryParseTemperature(text, out double temp))
                    {
                        if (temp >= minValue && temp <= maxValue)
                            return true;
                    }
                }
            }
            catch { /* 元素可能暂未就绪 */ }
            Thread.Sleep(500);
        }

        throw new TimeoutException(
            $"等待温度 [{textPrefix}] 达到 [{minValue}~{maxValue}]°C 超时");
    }

    /// <summary>等待系统消息出现（在消息区域文本中搜索关键词）</summary>
    public static bool WaitForMessage(Window window, string messageKeyword,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // 消息区域是 RichTextBox，在 UIA 中表现为 Document 控件
                var docElement = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
                if (docElement != null)
                {
                    var text = docElement.Properties.Name.ValueOrDefault;
                    if (text != null && text.Contains(messageKeyword))
                        return true;
                }

                // 降级：搜索所有包含消息关键词的元素
                var allElements = window.FindAllDescendants();
                foreach (var elem in allElements)
                {
                    var name = elem.Properties.Name.ValueOrDefault;
                    if (name != null && name.Contains(messageKeyword))
                    {
                        // 确认是在底部消息区域的元素
                        if (name.Contains(messageKeyword))
                            return true;
                    }
                }
            }
            catch { /* 元素可能暂未就绪 */ }
            Thread.Sleep(300);
        }

        throw new TimeoutException($"等待系统消息包含 '{messageKeyword}' 超时");
    }

    /// <summary>等待窗口出现</summary>
    public static Window WaitForWindow(AppLauncher launcher, string titlePart,
        TimeSpan? timeout = null)
    {
        var window = launcher.FindWindow(titlePart, timeout ?? TimeSpan.FromSeconds(10));
        if (window == null)
            throw new TimeoutException($"等待窗口 '{titlePart}' 超时");
        return window;
    }

    /// <summary>等待指定秒数（用于仿真等待）</summary>
    public static void WaitForSimulation(int seconds)
    {
        Thread.Sleep(seconds * 1000);
    }

    // ─── 私有辅助 ───

    private static bool TryParseTemperature(string text, out double temperature)
    {
        temperature = 0;
        // 从文本中提取数值，如 "炉温 1：750.5 °C"
        var parts = text.Split(new[] { '：', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var numPart = parts[1].Replace("°C", "").Replace("℃", "").Trim();
            return double.TryParse(numPart, out temperature);
        }
        return false;
    }
}
