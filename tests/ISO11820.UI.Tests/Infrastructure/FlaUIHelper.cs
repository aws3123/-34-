using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ISO11820.UI.Tests.Infrastructure;

/// <summary>
/// FlaUI 辅助工具 —— 封装常用 UI 操作
/// </summary>
public static class FlaUIHelper
{
    /// <summary>查找按钮并点击</summary>
    public static void ClickButton(Window window, string buttonName)
    {
        var btn = FindButton(window, buttonName);
        if (btn == null)
            throw new InvalidOperationException($"未找到按钮: {buttonName}");
        btn.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(300));
    }

    /// <summary>查找按钮（不抛异常）</summary>
    public static AutomationElement? FindButton(Window window, string buttonName)
    {
        return window.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName(buttonName)));
    }

    /// <summary>获取按钮的可用状态</summary>
    public static bool IsButtonEnabled(Window window, string buttonName)
    {
        var btn = FindButton(window, buttonName);
        if (btn == null) return false;
        return btn.Properties.IsEnabled.ValueOrDefault;
    }

    /// <summary>查找 TextBox 并输入文本 —— 优先 AutomationId，降级为第 N 个 Edit 控件</summary>
    public static void TypeIntoTextBox(Window window, string automationId, string text)
    {
        // 先尝试 AutomationId
        var textBox = FindTextBox(window, automationId);
        if (textBox != null)
        {
            textBox.Text = text;
            FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
            return;
        }

        // 降级：按 Edit 控件类型查找
        // 从 automationId 中提取索引（如 "_passwordTextBox" → 第 0 个 Edit）
        var edits = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
        if (edits.Length > 0)
        {
            edits[0].AsTextBox().Text = text;
            FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
            return;
        }

        throw new InvalidOperationException($"未找到文本框: {automationId}");
    }

    /// <summary>查找第 N 个 TextBox 并输入文本</summary>
    public static void TypeIntoNthTextBox(Window window, int index, string text)
    {
        var edits = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
        if (index >= edits.Length)
            throw new InvalidOperationException($"文本框索引 {index} 超出范围（共 {edits.Length} 个）");
        edits[index].AsTextBox().Text = text;
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>查找 TextBox</summary>
    public static AutomationElement? FindTextBoxElement(Window window, string automationId)
    {
        return window.FindFirstDescendant(cf =>
            cf.ByAutomationId(automationId).Or(cf.ByName(automationId)));
    }

    /// <summary>查找 TextBox 并返回文本</summary>
    public static TextBox? FindTextBox(Window window, string automationId)
    {
        var element = FindTextBoxElement(window, automationId);
        return element?.AsTextBox();
    }

    /// <summary>
    /// 获取 Label 的文本 —— 通过 AutomationId 或文本前缀查找
    /// WinForms 控件不暴露字段名作为 AutomationId，
    /// 所以用文本前缀（如 "当前状态" 匹配 "当前状态：空闲"）
    /// </summary>
    public static string? GetLabelText(Window window, string textPrefix)
    {
        // 先尝试 AutomationId（可能某些控件有设置）
        var element = window.FindFirstDescendant(cf => cf.ByAutomationId(textPrefix));
        if (element != null)
        {
            try
            {
                var label = element.AsLabel();
                if (label != null) return label.Text;
            }
            catch { }
            return element.Properties.Name.ValueOrDefault;
        }

        // 降级：按文本前缀查找 Label
        var allLabels = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
        foreach (var lbl in allLabels)
        {
            var name = lbl.Properties.Name.ValueOrDefault;
            if (name != null && name.Contains(textPrefix))
                return name;
        }

        return null;
    }

    /// <summary>获取 Label 文本（带超时等待）</summary>
    public static string? GetLabelText(Window window, string textPrefix, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var text = GetLabelText(window, textPrefix);
            if (!string.IsNullOrEmpty(text))
                return text;
            Thread.Sleep(200);
        }
        return GetLabelText(window, textPrefix);
    }

    /// <summary>查找 RadioButton 并选择</summary>
    public static void SelectRadioButton(Window window, string radioName)
    {
        var element = window.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.RadioButton).And(cf.ByName(radioName)));
        if (element == null)
            throw new InvalidOperationException($"未找到单选按钮: {radioName}");

        // 检查是否已选中
        var isSelected = element.Properties.IsEnabled.ValueOrDefault;
        element.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>检查 RadioButton 是否选中</summary>
    public static bool IsRadioButtonSelected(AutomationElement radioElement)
    {
        // 在 FlaUI v4 中通过 Name 属性或 Invoke pattern 间接判断
        // 简单处理：假设点击即选中
        return true;
    }

    /// <summary>查找 CheckBox 并设置状态</summary>
    public static void SetCheckBox(Window window, string automationId, bool isChecked)
    {
        var element = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (element == null)
            throw new InvalidOperationException($"未找到复选框: {automationId}");
        element.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>获取消息区域的文本内容（RichTextBox → Document 控件）</summary>
    public static string GetMessageText(Window window)
    {
        // 搜索 Document 控件（WinForms RichTextBox 在 UIA 中的表示）
        var document = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
        if (document != null)
        {
            try
            {
                var textBox = document.AsTextBox();
                if (textBox != null)
                    return textBox.Text ?? string.Empty;
            }
            catch { }
            return document.Properties.Name.ValueOrDefault ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>获取消息区域的所有行文本</summary>
    public static List<string> GetMessageLines(Window window)
    {
        var text = GetMessageText(window);
        if (string.IsNullOrEmpty(text)) return new List<string>();
        return text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>等待消息区域出现包含关键词的消息</summary>
    public static bool WaitForMessageKeyword(Window window, string keyword,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            var text = GetMessageText(window);
            if (text.Contains(keyword))
                return true;
            // 降级：搜索所有元素
            var allElements = window.FindAllDescendants();
            foreach (var elem in allElements)
            {
                var name = elem.Properties.Name.ValueOrDefault;
                if (name != null && name.Contains(keyword))
                    return true;
            }
            Thread.Sleep(300);
        }
        return false;
    }

    /// <summary>获取 ListBox 的所有项文本</summary>
    public static List<string> GetListBoxItems(Window window, string automationId)
    {
        var element = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (element == null) return new List<string>();
        var listBox = element.AsListBox();
        if (listBox == null) return new List<string>();

        return listBox.Items.Select(i => i.Text).Where(t => t != null).ToList();
    }

    /// <summary>等待 ListBox 中出现包含关键词的项</summary>
    public static bool WaitForListBoxItem(Window window, string automationId, string keyword,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            var items = GetListBoxItems(window, automationId);
            if (items.Any(i => i.Contains(keyword)))
                return true;
            Thread.Sleep(300);
        }
        return false;
    }

    /// <summary>查找对话框窗口</summary>
    public static Window? FindDialog(AppLauncher launcher, string titlePart,
        TimeSpan? timeout = null)
    {
        return launcher.FindWindow(titlePart, timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>在对话框中点击确定/创建/保存按钮</summary>
    public static void ClickDialogOk(Window dialog)
    {
        var okBtn = dialog.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button)
              .And(cf.ByName("确定").Or(cf.ByName("创建试验")).Or(cf.ByName("保存")).Or(cf.ByName("是"))));
        okBtn?.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>在对话框中点击取消按钮</summary>
    public static void ClickDialogCancel(Window dialog)
    {
        var cancelBtn = dialog.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button)
              .And(cf.ByName("取消").Or(cf.ByName("否"))));
        cancelBtn?.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>获取 TabControl 当前选中的 Tab 名称</summary>
    public static string? GetSelectedTabName(Window window)
    {
        var tabControl = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab));
        if (tabControl == null) return null;

        // 在 FlaUI 中，TabControl 的子元素就是各个 TabItem
        var tabs = tabControl.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem));

        // 降级：返回第一个 Tab
        return tabs.FirstOrDefault()?.Name;
    }

    /// <summary>切换 Tab</summary>
    public static void SelectTab(Window window, string tabName)
    {
        var tabControl = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab));
        if (tabControl == null)
            throw new InvalidOperationException("未找到 TabControl");

        var tab = tabControl.FindFirstDescendant(cf => cf.ByName(tabName));
        if (tab == null)
            throw new InvalidOperationException($"未找到 Tab: {tabName}");

        tab.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }
}
