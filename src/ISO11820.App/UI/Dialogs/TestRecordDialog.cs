using ISO11820.App.Features.TestRecord;
using ISO11820.App.Shared.Models.Records;

namespace ISO11820.App.UI.Dialogs;

public sealed class TestRecordDialog : Form
{
    private readonly TestRecordCoordinator _coordinator;
    private readonly string _testId;
    private readonly string _productId;
    private readonly double _preWeight;
    private readonly double _envTemp;
    private readonly double _finalFurnace1;
    private readonly double _finalFurnace2;
    private readonly double _finalSurface;
    private readonly double _finalCenter;

    private TextBox _txtOperator = null!;
    private TextBox _txtPhenomenon = null!;
    private ComboBox _cmbQuality = null!;
    private TextBox _txtRemarks = null!;

    // 火焰信息
    private CheckBox _chkFlame = null!;
    private TextBox _txtFlameTime = null!;
    private TextBox _txtFlameDuration = null!;

    // 试验后质量
    private TextBox _txtPostWeight = null!;

    // 计算结果标签
    private Label _lblLostWeight = null!;
    private Label _lblLostWeightPercent = null!;
    private Label _lblDeltaTf = null!;
    private Label _lblDeltaFurnace1 = null!;
    private Label _lblDeltaFurnace2 = null!;
    private Label _lblDeltaCenter = null!;
    private Label _lblJudgment = null!;

    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;

    public bool SaveSucceeded { get; private set; }

    // 计算属性
    public double LostWeight => _preWeight - GetPostWeight();
    public double LostWeightPercent => _preWeight > 0 ? LostWeight / _preWeight * 100 : 0;
    public double DeltaTf => _finalSurface - _envTemp;
    public double DeltaFurnace1 => _finalFurnace1 - _envTemp;
    public double DeltaFurnace2 => _finalFurnace2 - _envTemp;
    public double DeltaCenter => _finalCenter - _envTemp;

    public TestRecordDialog(TestRecordCoordinator coordinator, string testId, string productId,
        double preWeight = 0, double envTemp = 25.0,
        double finalFurnace1 = 0, double finalFurnace2 = 0,
        double finalSurface = 0, double finalCenter = 0)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _testId = testId ?? throw new ArgumentNullException(nameof(testId));
        _productId = productId ?? throw new ArgumentNullException(nameof(productId));
        _preWeight = preWeight;
        _envTemp = envTemp;
        _finalFurnace1 = finalFurnace1;
        _finalFurnace2 = finalFurnace2;
        _finalSurface = finalSurface;
        _finalCenter = finalCenter;

        Text = "试验记录";
        Width = 620;
        Height = 640;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei", 10F);

        InitializeComponents();
        LoadPendingData();
    }

    private void InitializeComponents()
    {
        var padding = 20;
        var labelWidth = 140;
        var inputWidth = 420;
        var rowHeight = 34;
        var y = padding;

        // === 试验信息 ===
        var lblInfo = new Label
        {
            Location = new Point(padding, y),
            Size = new Size(inputWidth, rowHeight),
            Text = $"试验编号: {_testId}  |  产品编号: {_productId}",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 100, 180),
        };
        Controls.Add(lblInfo);
        y += rowHeight + 8;

        // === 基本信息 ===
        AddSectionLabel("基本信息", ref y, padding, inputWidth);

        var lblOperator = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "操作员：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtOperator = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
        };
        Controls.AddRange(new Control[] { lblOperator, _txtOperator });
        y += rowHeight;

        var lblPhenomenon = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "试验现象：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtPhenomenon = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight * 3),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };
        Controls.AddRange(new Control[] { lblPhenomenon, _txtPhenomenon });
        y += rowHeight * 3;

        var lblQuality = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "试验质量：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _cmbQuality = new ComboBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cmbQuality.Items.AddRange(new object[]
        {
            new QualityItem(TestQuality.NotEvaluated, "请选择..."),
            new QualityItem(TestQuality.Pass, "合格"),
            new QualityItem(TestQuality.Fail, "不合格"),
            new QualityItem(TestQuality.Retest, "需复检"),
        });
        _cmbQuality.SelectedIndex = 0;
        Controls.AddRange(new Control[] { lblQuality, _cmbQuality });
        y += rowHeight + 8;

        // === 火焰信息 ===
        AddSectionLabel("火焰信息", ref y, padding, inputWidth);

        _chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(padding + labelWidth, y),
            AutoSize = true,
        };
        _chkFlame.CheckedChanged += OnFlameCheckedChanged;
        Controls.Add(_chkFlame);
        y += rowHeight;

        var lblFlameTime = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "火焰出现时间 (秒)：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtFlameTime = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(100, rowHeight),
            Text = "0",
            Enabled = false,
        };
        Controls.AddRange(new Control[] { lblFlameTime, _txtFlameTime });
        y += rowHeight;

        var lblFlameDuration = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "火焰持续时间 (秒)：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtFlameDuration = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(100, rowHeight),
            Text = "0",
            Enabled = false,
        };
        Controls.AddRange(new Control[] { lblFlameDuration, _txtFlameDuration });
        y += rowHeight + 8;

        // === 试验后质量 ===
        AddSectionLabel("试验后质量", ref y, padding, inputWidth);

        var lblPostWeight = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "试验后质量 (g)：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtPostWeight = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(150, rowHeight),
        };
        _txtPostWeight.TextChanged += OnPostWeightChanged;
        Controls.AddRange(new Control[] { lblPostWeight, _txtPostWeight });
        y += rowHeight + 8;

        // === 计算结果 ===
        AddSectionLabel("计算结果", ref y, padding, inputWidth);

        _lblLostWeight = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = "失重量：-- g",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblLostWeight);
        y += rowHeight;

        _lblLostWeightPercent = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = "失重率：-- %",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblLostWeightPercent);
        y += rowHeight;

        _lblDeltaFurnace1 = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = $"炉温1温升：{_finalFurnace1 - _envTemp:F1} °C",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblDeltaFurnace1);
        y += rowHeight;

        _lblDeltaFurnace2 = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = $"炉温2温升：{_finalFurnace2 - _envTemp:F1} °C",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblDeltaFurnace2);
        y += rowHeight;

        _lblDeltaTf = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = $"表面温升：{_finalSurface - _envTemp:F1} °C",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblDeltaTf);
        y += rowHeight;

        _lblDeltaCenter = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = $"中心温升：{_finalCenter - _envTemp:F1} °C",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblDeltaCenter);
        y += rowHeight;

        _lblJudgment = new Label
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            Text = "判定：--",
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblJudgment);
        y += rowHeight + 8;

        // === 备注 ===
        var lblRemarks = new Label
        {
            Location = new Point(padding, y + 3),
            Size = new Size(labelWidth, rowHeight),
            Text = "备注：",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtRemarks = new TextBox
        {
            Location = new Point(padding + labelWidth, y),
            Size = new Size(inputWidth - labelWidth, rowHeight * 2),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };
        Controls.AddRange(new Control[] { lblRemarks, _txtRemarks });
        y += rowHeight * 2 + 8;

        // === 状态标签 ===
        _lblStatus = new Label
        {
            Location = new Point(padding, y),
            Size = new Size(inputWidth, rowHeight),
            Text = "",
            ForeColor = Color.Red,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblStatus);
        y += rowHeight;

        // === 按钮 ===
        _btnSave = new Button
        {
            Text = "保存",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(Width - 230, y + 5),
            Size = new Size(100, 36),
        };
        _btnSave.Click += OnSaveClick;

        _btnCancel = new Button
        {
            Text = "取消",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(Width - 120, y + 5),
            Size = new Size(90, 36),
            DialogResult = DialogResult.Cancel,
        };
        _btnCancel.Click += OnCancelClick;

        Controls.AddRange(new Control[] { _btnSave, _btnCancel });

        // 动态调整窗体高度
        Height = y + 80;

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
        ActiveControl = _txtOperator;
    }

    private void AddSectionLabel(string text, ref int y, int x, int width)
    {
        var label = new Label
        {
            Text = text,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(width, 24),
            ForeColor = Color.FromArgb(0, 100, 180),
        };
        Controls.Add(label);
        y += 28;
    }

    private void OnFlameCheckedChanged(object? sender, EventArgs e)
    {
        _txtFlameTime.Enabled = _chkFlame.Checked;
        _txtFlameDuration.Enabled = _chkFlame.Checked;
    }

    private double GetPostWeight()
    {
        return double.TryParse(_txtPostWeight.Text, out var w) && w > 0 ? w : 0;
    }

    private void OnPostWeightChanged(object? sender, EventArgs e)
    {
        UpdateComputedLabels();
    }

    private void UpdateComputedLabels()
    {
        var postWeight = GetPostWeight();
        var lostWeight = _preWeight - postWeight;
        var lostWeightPct = _preWeight > 0 ? lostWeight / _preWeight * 100 : 0;
        var deltaTf = _finalSurface - _envTemp;
        var deltaF1 = _finalFurnace1 - _envTemp;
        var deltaF2 = _finalFurnace2 - _envTemp;
        var deltaCtr = _finalCenter - _envTemp;

        _lblLostWeight.Text = $"失重量：{lostWeight:F2} g";
        _lblLostWeightPercent.Text = $"失重率：{lostWeightPct:F2} %";
        _lblDeltaFurnace1.Text = $"炉温1温升：{deltaF1:F1} °C";
        _lblDeltaFurnace2.Text = $"炉温2温升：{deltaF2:F1} °C";
        _lblDeltaTf.Text = $"表面温升：{deltaTf:F1} °C";
        _lblDeltaCenter.Text = $"中心温升：{deltaCtr:F1} °C";

        // 判定逻辑：表面温升 ≤ 50°C 且 失重率 ≤ 50% 且 火焰持续时间 < 5秒
        if (postWeight <= 0)
        {
            _lblJudgment.Text = "判定：--";
            _lblJudgment.ForeColor = Color.Gray;
            return;
        }

        var flameDur = 0;
        if (_chkFlame.Checked)
        {
            int.TryParse(_txtFlameDuration.Text, out flameDur);
        }

        bool pass = deltaTf <= 50 && lostWeightPct <= 50 && flameDur < 5;
        _lblJudgment.Text = pass ? "判定：通过" : "判定：不通过";
        _lblJudgment.ForeColor = pass ? Color.Green : Color.Red;
    }

    private void LoadPendingData()
    {
        var pending = _coordinator.GetPendingRecord(_testId);
        if (pending != null)
        {
            _txtOperator.Text = pending.Operator;
            _txtPhenomenon.Text = pending.Phenomenon;
            _txtRemarks.Text = pending.Remarks;
            _txtPostWeight.Text = pending.PostWeightGrams > 0
                ? pending.PostWeightGrams.ToString("F2")
                : "";

            _chkFlame.Checked = pending.HasFlame;
            _txtFlameTime.Text = pending.FlameTimeSeconds.ToString();
            _txtFlameDuration.Text = pending.FlameDurationSeconds.ToString();

            for (int i = 0; i < _cmbQuality.Items.Count; i++)
            {
                if (((QualityItem)_cmbQuality.Items[i]!).Quality == pending.Quality)
                {
                    _cmbQuality.SelectedIndex = i;
                    break;
                }
            }
        }

        // 初始化时更新计算标签
        UpdateComputedLabels();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        var selectedQuality = ((QualityItem)_cmbQuality.SelectedItem!).Quality;

        // 解析火焰数据
        var hasFlame = _chkFlame.Checked;
        var flameTimeSeconds = 0;
        var flameDurationSeconds = 0;

        if (hasFlame)
        {
            if (!int.TryParse(_txtFlameTime.Text, out flameTimeSeconds) || flameTimeSeconds < 0)
            {
                _lblStatus.Text = "请输入有效的火焰出现时间（秒）";
                return;
            }
            if (!int.TryParse(_txtFlameDuration.Text, out flameDurationSeconds) || flameDurationSeconds < 0)
            {
                _lblStatus.Text = "请输入有效的火焰持续时间（秒）";
                return;
            }
        }

        // 解析试验后质量
        if (!double.TryParse(_txtPostWeight.Text, out var postWeight) || postWeight <= 0)
        {
            _lblStatus.Text = "请输入有效的试验后质量";
            _txtPostWeight.Focus();
            return;
        }

        var input = new TestRecordInput
        {
            TestId = _testId,
            ProductId = _productId,
            Operator = _txtOperator.Text.Trim(),
            Phenomenon = _txtPhenomenon.Text.Trim(),
            Quality = selectedQuality,
            Remarks = _txtRemarks.Text.Trim(),
            RecordedAt = DateTime.Now,
            HasFlame = hasFlame,
            FlameTimeSeconds = flameTimeSeconds,
            FlameDurationSeconds = flameDurationSeconds,
            PostWeightGrams = postWeight,
            PreWeightGrams = _preWeight,
            LostWeightGrams = _preWeight - postWeight,
            LostWeightPercent = _preWeight > 0 ? (_preWeight - postWeight) / _preWeight * 100 : 0,
            EnvTemperature = _envTemp,
            FinalFurnace1 = _finalFurnace1,
            FinalFurnace2 = _finalFurnace2,
            FinalSurface = _finalSurface,
            FinalCenter = _finalCenter,
            DeltaFurnace1 = _finalFurnace1 - _envTemp,
            DeltaFurnace2 = _finalFurnace2 - _envTemp,
            DeltaSurface = _finalSurface - _envTemp,
            DeltaCenter = _finalCenter - _envTemp,
        };

        var validation = _coordinator.ValidateRecord(input);
        if (!validation.IsValid)
        {
            _lblStatus.Text = "请完善必填项：" + string.Join("、", validation.Errors);
            return;
        }

        if (_coordinator.IsRecordSaved(_testId))
        {
            _lblStatus.Text = "该试验记录已保存，请勿重复提交";
            return;
        }

        var acceptResult = _coordinator.AcceptRecord(input);
        if (!acceptResult.Accepted)
        {
            _lblStatus.Text = acceptResult.Message ?? "记录接收失败";
            return;
        }

        _lblStatus.ForeColor = Color.Green;
        _lblStatus.Text = "记录已接收，等待持久化层保存...";

        SaveSucceeded = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    public TestRecordInput? GetCurrentRecord()
    {
        return _coordinator.GetPendingRecord(_testId);
    }

    private sealed class QualityItem
    {
        public TestQuality Quality { get; }
        public string DisplayText { get; }

        public QualityItem(TestQuality quality, string displayText)
        {
            Quality = quality;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText;
    }
}
