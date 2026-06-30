namespace ISO11820.App.UI.Dialogs;

/// <summary>
/// 新建试验对话框 — 收集试验所需的所有信息
/// </summary>
public sealed class NewTestDialog : Form
{
    // 环境信息
    private readonly TextBox _envTempTextBox;
    private readonly TextBox _envHumidityTextBox;

    // 样品信息
    private readonly TextBox _productIdTextBox;
    private readonly TextBox _testIdTextBox;
    private readonly TextBox _sampleNameTextBox;
    private readonly TextBox _specTextBox;
    private readonly TextBox _heightTextBox;
    private readonly TextBox _diameterTextBox;

    // 试验参数
    private readonly TextBox _operatorTextBox;
    private readonly RadioButton _standardTimeRadio;
    private readonly RadioButton _customTimeRadio;
    private readonly TextBox _customMinutesTextBox;

    // 质量
    private readonly TextBox _preWeightTextBox;

    // 设备信息（自动填入，只读）
    private readonly TextBox _deviceIdTextBox;
    private readonly TextBox _deviceNameTextBox;
    private readonly TextBox _certDateTextBox;
    private readonly TextBox _constPowerTextBox;

    // 备注
    private readonly TextBox _notesTextBox;

    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public TestCreateInfo? TestInfo { get; private set; }

    public NewTestDialog(string operatorName)
    {
        Text = "新建试验";
        Size = new Size(650, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei", 10F);
        AutoScaleMode = AutoScaleMode.Font;

        var y = 15;
        var labelX = 20;
        var inputX = 180;
        var inputWidth = 200;
        var rowHeight = 35;

        // === 环境信息 ===
        AddSectionLabel("环境信息", ref y);

        AddLabelInput("环境温度 (°C)：", ref y, labelX, inputX, out _envTempTextBox, inputWidth);
        _envTempTextBox.Text = "25.0";
        AddLabelInput("环境湿度 (%)：", ref y, labelX, inputX, out _envHumidityTextBox, inputWidth);
        _envHumidityTextBox.Text = "50.0";

        y += 10;

        // === 样品信息 ===
        AddSectionLabel("样品信息", ref y);

        AddLabelInput("样品编号：", ref y, labelX, inputX, out _productIdTextBox, inputWidth);
        AddLabelInput("试验标识：", ref y, labelX, inputX, out _testIdTextBox, inputWidth);
        AddLabelInput("样品名称：", ref y, labelX, inputX, out _sampleNameTextBox, inputWidth);
        AddLabelInput("规格：", ref y, labelX, inputX, out _specTextBox, inputWidth);

        // 高度和直径在同一行
        var heightLabel = new Label
        {
            Text = "高度 (mm)：",
            Location = new Point(labelX, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _heightTextBox = new TextBox { Location = new Point(inputX, y), Size = new Size(80, 28), Text = "0" };

        var diameterLabel = new Label
        {
            Text = "直径 (mm)：",
            Location = new Point(290, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _diameterTextBox = new TextBox { Location = new Point(420, y), Size = new Size(80, 28), Text = "0" };

        Controls.AddRange(new Control[] { heightLabel, _heightTextBox, diameterLabel, _diameterTextBox });
        y += rowHeight;

        y += 10;

        // === 试验参数 ===
        AddSectionLabel("试验参数", ref y);

        AddLabelInput("操作员：", ref y, labelX, inputX, out _operatorTextBox, inputWidth);
        _operatorTextBox.Text = operatorName;

        // 试验时长模式
        var durationLabel = new Label
        {
            Text = "试验时长：",
            Location = new Point(labelX, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _standardTimeRadio = new RadioButton
        {
            Text = "标准 60 分钟",
            Location = new Point(inputX, y),
            AutoSize = true,
            Checked = true,
        };
        _customTimeRadio = new RadioButton
        {
            Text = "自定义",
            Location = new Point(inputX + 150, y),
            AutoSize = true,
        };
        _customMinutesTextBox = new TextBox
        {
            Location = new Point(inputX + 260, y),
            Size = new Size(60, 28),
            Enabled = false,
            Text = "30",
        };
        _customTimeRadio.CheckedChanged += (_, _) => _customMinutesTextBox.Enabled = _customTimeRadio.Checked;

        Controls.AddRange(new Control[] { durationLabel, _standardTimeRadio, _customTimeRadio, _customMinutesTextBox });
        y += rowHeight;

        AddLabelInput("试验前质量 (g)：", ref y, labelX, inputX, out _preWeightTextBox, inputWidth);

        y += 10;

        // === 设备信息（自动填入） ===
        AddSectionLabel("设备信息（自动填入）", ref y);

        AddLabelInput("设备编号：", ref y, labelX, inputX, out _deviceIdTextBox, inputWidth);
        _deviceIdTextBox.ReadOnly = true;
        _deviceIdTextBox.Text = "SN-001";
        AddLabelInput("设备名称：", ref y, labelX, inputX, out _deviceNameTextBox, inputWidth);
        _deviceNameTextBox.ReadOnly = true;
        _deviceNameTextBox.Text = "ISO11820 Test Furnace";
        AddLabelInput("检定日期：", ref y, labelX, inputX, out _certDateTextBox, inputWidth);
        _certDateTextBox.ReadOnly = true;
        _certDateTextBox.Text = DateTime.Now.ToString("yyyy-MM-dd");
        AddLabelInput("恒功率值：", ref y, labelX, inputX, out _constPowerTextBox, inputWidth);
        _constPowerTextBox.ReadOnly = true;
        _constPowerTextBox.Text = "2048";

        y += 10;

        // === 备注 ===
        var notesLabel = new Label
        {
            Text = "备注：",
            Location = new Point(labelX, y),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _notesTextBox = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(410, 60),
            Multiline = true,
        };
        Controls.AddRange(new Control[] { notesLabel, _notesTextBox });
        y += 70;

        // === 按钮 ===
        _okButton = new Button
        {
            Text = "创建试验",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(380, y + 5),
            Size = new Size(110, 38),
        };
        _okButton.Click += OnOkClick;

        _cancelButton = new Button
        {
            Text = "取消",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(505, y + 5),
            Size = new Size(95, 38),
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[] { _okButton, _cancelButton });

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
        ActiveControl = _productIdTextBox;
    }

    private void AddSectionLabel(string text, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
            Location = new Point(20, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(0, 100, 180),
        };
        Controls.Add(label);
        y += 28;
    }

    private void AddLabelInput(string labelText, ref int y, int labelX, int inputX, out TextBox textBox, int inputWidth)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(labelX, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        textBox = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputWidth, 28),
        };
        Controls.AddRange(new Control[] { label, textBox });
        y += 35;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        // 必填校验
        if (string.IsNullOrWhiteSpace(_productIdTextBox.Text))
        {
            MessageBox.Show("请输入样品编号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _productIdTextBox.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_testIdTextBox.Text))
        {
            MessageBox.Show("请输入试验标识。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _testIdTextBox.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_sampleNameTextBox.Text))
        {
            MessageBox.Show("请输入样品名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _sampleNameTextBox.Focus();
            return;
        }
        if (!double.TryParse(_preWeightTextBox.Text, out var preWeight) || preWeight <= 0)
        {
            MessageBox.Show("请输入有效的试验前质量。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _preWeightTextBox.Focus();
            return;
        }

        int durationSeconds;
        if (_standardTimeRadio.Checked)
        {
            durationSeconds = 3600; // 60 分钟
        }
        else
        {
            if (!int.TryParse(_customMinutesTextBox.Text, out var minutes) || minutes <= 0)
            {
                MessageBox.Show("请输入有效的自定义时长（分钟）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _customMinutesTextBox.Focus();
                return;
            }
            durationSeconds = minutes * 60;
        }

        TestInfo = new TestCreateInfo(
            ProductId: _productIdTextBox.Text.Trim(),
            TestId: _testIdTextBox.Text.Trim(),
            SampleName: _sampleNameTextBox.Text.Trim(),
            Specification: _specTextBox.Text.Trim(),
            HeightMm: double.TryParse(_heightTextBox.Text, out var h) ? h : 0,
            DiameterMm: double.TryParse(_diameterTextBox.Text, out var d) ? d : 0,
            OperatorName: _operatorTextBox.Text.Trim(),
            DurationSeconds: durationSeconds,
            PreWeightGrams: preWeight,
            EnvTemperature: double.TryParse(_envTempTextBox.Text, out var t) ? t : 25.0,
            EnvHumidity: double.TryParse(_envHumidityTextBox.Text, out var hum) ? hum : 50.0,
            DeviceId: _deviceIdTextBox.Text.Trim(),
            DeviceName: _deviceNameTextBox.Text.Trim(),
            CertDate: _certDateTextBox.Text.Trim(),
            ConstPower: double.TryParse(_constPowerTextBox.Text, out var cp) ? cp : 2048,
            Notes: _notesTextBox.Text.Trim());

        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// 试验创建信息
/// </summary>
public sealed record TestCreateInfo(
    string ProductId,
    string TestId,
    string SampleName,
    string Specification,
    double HeightMm,
    double DiameterMm,
    string OperatorName,
    int DurationSeconds,
    double PreWeightGrams,
    double EnvTemperature,
    double EnvHumidity,
    string DeviceId,
    string DeviceName,
    string CertDate,
    double ConstPower,
    string Notes);
