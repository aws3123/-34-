using ISO11820.App.Config;
using ISO11820.App.UI.Common;

namespace ISO11820.App.UI.Dialogs;

/// <summary>
/// 参数设置对话框，允许用户修改仿真参数（会话内有效）
/// </summary>
public sealed class ParameterSettingsDialog : Form
{
    private readonly TextBox _heatingRateTextBox;
    private readonly TextBox _targetTempTextBox;
    private readonly TextBox _stableThresholdTextBox;
    private readonly TextBox _fluctuationTextBox;

    public SimulationSettings? UpdatedSettings { get; private set; }

    public ParameterSettingsDialog(SimulationSettings current)
    {
        Text = "参数设置";
        Size = new Size(480, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei", 10F);
        AutoScaleMode = AutoScaleMode.Font;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Row 0: Heating Rate
        panel.Controls.Add(CreateLabel("升温速率 (°C/s):"), 0, 0);
        _heatingRateTextBox = CreateTextBox(current.HeatingRatePerSecond);
        panel.Controls.Add(_heatingRateTextBox, 1, 0);

        // Row 1: Target Temp
        panel.Controls.Add(CreateLabel("目标温度 (°C):"), 0, 1);
        _targetTempTextBox = CreateTextBox(current.TargetTemperature);
        panel.Controls.Add(_targetTempTextBox, 1, 1);

        // Row 2: Stable Threshold
        panel.Controls.Add(CreateLabel("稳定阈值 (°C):"), 0, 2);
        _stableThresholdTextBox = CreateTextBox(current.StableThreshold);
        panel.Controls.Add(_stableThresholdTextBox, 1, 2);

        // Row 3: Fluctuation
        panel.Controls.Add(CreateLabel("温度波动 (°C):"), 0, 3);
        _fluctuationTextBox = CreateTextBox(current.TempFluctuation);
        panel.Controls.Add(_fluctuationTextBox, 1, 3);

        // Row 4: Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
        var okButton = new Button { Text = "确定", Width = 80 };
        okButton.Click += OnOkClick;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
        panel.Controls.Add(buttonPanel, 0, 4);
        panel.SetColumnSpan(buttonPanel, 2);

        Controls.Add(panel);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 12, 6),
        };
    }

    private static TextBox CreateTextBox(double value)
    {
        return new TextBox
        {
            Text = value.ToString("F2"),
            Dock = DockStyle.Fill
        };
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (!double.TryParse(_heatingRateTextBox.Text, out var heatingRate) ||
            !double.TryParse(_targetTempTextBox.Text, out var targetTemp) ||
            !double.TryParse(_stableThresholdTextBox.Text, out var stableThreshold) ||
            !double.TryParse(_fluctuationTextBox.Text, out var fluctuation))
        {
            MessageBox.Show(this, "请输入有效的数字", "输入错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var (isValid, errors) = ParameterValidator.Validate(
            heatingRate, targetTemp, stableThreshold, fluctuation);

        if (!isValid)
        {
            MessageBox.Show(this, string.Join("\n", errors), "参数验证失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        UpdatedSettings = new SimulationSettings
        {
            EnableSimulation = true,
            StartTemperature = 25.0,
            HeatingRatePerSecond = heatingRate,
            TargetTemperature = targetTemp,
            StableThreshold = stableThreshold,
            TempFluctuation = fluctuation
        };

        DialogResult = DialogResult.OK;
    }
}
