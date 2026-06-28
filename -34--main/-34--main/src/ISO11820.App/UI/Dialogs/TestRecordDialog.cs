using ISO11820.App.Features.TestRecord;
using ISO11820.App.Shared.Models.Records;

namespace ISO11820.App.UI.Dialogs;

/// <summary>
/// 试验记录对话框
/// 承接试验现象、试验后质量输入和保存动作
/// 不直接操作数据库，而是通过 TestRecordCoordinator 完成保存
/// </summary>
public sealed class TestRecordDialog : Form
{
    private readonly TestRecordCoordinator _coordinator;
    private readonly string _testId;
    private readonly string _productId;

    // UI 控件
    private TextBox _txtPhenomenon = null!;
    private ComboBox _cmbQuality = null!;
    private TextBox _txtRemarks = null!;
    private TextBox _txtOperator = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;

    /// <summary>
    /// 保存是否成功
    /// </summary>
    public bool SaveSucceeded { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="coordinator">记录协调器</param>
    /// <param name="testId">试验编号</param>
    /// <param name="productId">产品编号</param>
    public TestRecordDialog(TestRecordCoordinator coordinator, string testId, string productId)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _testId = testId ?? throw new ArgumentNullException(nameof(testId));
        _productId = productId ?? throw new ArgumentNullException(nameof(productId));

        Text = "试验记录";
        Width = 560;
        Height = 480;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        InitializeComponents();
        LoadPendingData();
    }

    /// <summary>
    /// 初始化 UI 组件
    /// </summary>
    private void InitializeComponents()
    {
        var padding = 16;
        var labelWidth = 80;
        var inputWidth = 420;
        var rowHeight = 32;

        // 试验信息标签
        var lblInfo = new Label
        {
            Location = new Point(padding, padding),
            Size = new Size(inputWidth, rowHeight),
            Text = $"试验编号: {_testId}  |  产品编号: {_productId}",
            Font = new Font(Font, FontStyle.Bold)
        };

        // 操作员
        var lblOperator = new Label
        {
            Location = new Point(padding, padding + rowHeight * 1),
            Size = new Size(labelWidth, rowHeight),
            Text = "操作员：",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtOperator = new TextBox
        {
            Location = new Point(padding + labelWidth, padding + rowHeight * 1 + 4),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            TabIndex = 0
        };

        // 试验现象
        var lblPhenomenon = new Label
        {
            Location = new Point(padding, padding + rowHeight * 2 + 8),
            Size = new Size(labelWidth, rowHeight),
            Text = "试验现象：",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtPhenomenon = new TextBox
        {
            Location = new Point(padding + labelWidth, padding + rowHeight * 2 + 8),
            Size = new Size(inputWidth - labelWidth, rowHeight * 3),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            TabIndex = 1
        };

        // 试验后质量
        var lblQuality = new Label
        {
            Location = new Point(padding, padding + rowHeight * 6 + 8),
            Size = new Size(labelWidth, rowHeight),
            Text = "试验质量：",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cmbQuality = new ComboBox
        {
            Location = new Point(padding + labelWidth, padding + rowHeight * 6 + 4),
            Size = new Size(inputWidth - labelWidth, rowHeight),
            DropDownStyle = ComboBoxStyle.DropDownList,
            TabIndex = 2
        };
        _cmbQuality.Items.AddRange(new object[]
        {
            new QualityItem(TestQuality.NotEvaluated, "请选择..."),
            new QualityItem(TestQuality.Pass, "合格"),
            new QualityItem(TestQuality.Fail, "不合格"),
            new QualityItem(TestQuality.Retest, "需复检")
        });
        _cmbQuality.SelectedIndex = 0;

        // 备注
        var lblRemarks = new Label
        {
            Location = new Point(padding, padding + rowHeight * 7 + 8),
            Size = new Size(labelWidth, rowHeight),
            Text = "备注：",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtRemarks = new TextBox
        {
            Location = new Point(padding + labelWidth, padding + rowHeight * 7 + 8),
            Size = new Size(inputWidth - labelWidth, rowHeight * 3),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            TabIndex = 3
        };

        // 状态标签
        _lblStatus = new Label
        {
            Location = new Point(padding, padding + rowHeight * 11 + 8),
            Size = new Size(inputWidth, rowHeight),
            Text = "",
            ForeColor = Color.Red,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 按钮
        _btnSave = new Button
        {
            Text = "保存",
            Location = new Point(Width - 220, Height - 80),
            Size = new Size(90, 32),
            TabIndex = 4
        };
        _btnSave.Click += OnSaveClick;

        _btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(Width - 120, Height - 80),
            Size = new Size(90, 32),
            TabIndex = 5,
            DialogResult = DialogResult.Cancel
        };
        _btnCancel.Click += OnCancelClick;

        // 添加控件
        Controls.Add(lblInfo);
        Controls.Add(lblOperator);
        Controls.Add(_txtOperator);
        Controls.Add(lblPhenomenon);
        Controls.Add(_txtPhenomenon);
        Controls.Add(lblQuality);
        Controls.Add(_cmbQuality);
        Controls.Add(lblRemarks);
        Controls.Add(_txtRemarks);
        Controls.Add(_lblStatus);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    /// <summary>
    /// 加载待保存的预填数据
    /// </summary>
    private void LoadPendingData()
    {
        var pending = _coordinator.GetPendingRecord(_testId);
        if (pending != null)
        {
            _txtOperator.Text = pending.Operator;
            _txtPhenomenon.Text = pending.Phenomenon;
            _txtRemarks.Text = pending.Remarks;

            for (int i = 0; i < _cmbQuality.Items.Count; i++)
            {
                if (((QualityItem)_cmbQuality.Items[i]).Quality == pending.Quality)
                {
                    _cmbQuality.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    private void OnSaveClick(object? sender, EventArgs e)
    {
        // 获取选中的质量评估
        var selectedQuality = ((QualityItem)_cmbQuality.SelectedItem).Quality;

        // 构建记录输入
        var input = new TestRecordInput
        {
            TestId = _testId,
            ProductId = _productId,
            Operator = _txtOperator.Text.Trim(),
            Phenomenon = _txtPhenomenon.Text.Trim(),
            Quality = selectedQuality,
            Remarks = _txtRemarks.Text.Trim(),
            RecordedAt = DateTime.Now
        };

        // 验证输入
        var validation = _coordinator.ValidateRecord(input);
        if (!validation.IsValid)
        {
            _lblStatus.Text = "请完善必填项：" + string.Join("、", validation.Errors);
            return;
        }

        // 检查是否已保存
        if (_coordinator.IsRecordSaved(_testId))
        {
            _lblStatus.Text = "该试验记录已保存，请勿重复提交";
            return;
        }

        // 接收记录
        var acceptResult = _coordinator.AcceptRecord(input);
        if (!acceptResult.Accepted)
        {
            _lblStatus.Text = acceptResult.Message ?? "记录接收失败";
            return;
        }

        // 提示需要调用持久化层
        // 实际的 ExecuteSave 调用应由外部传入的 persistenceAction 完成
        // 这里仅演示流程
        _lblStatus.ForeColor = Color.Green;
        _lblStatus.Text = "记录已接收，等待持久化层保存...";

        SaveSucceeded = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    /// <summary>
    /// 获取当前录入的记录输入
    /// 用于外部调用 ExecuteSave 时传入
    /// </summary>
    public TestRecordInput? GetCurrentRecord()
    {
        return _coordinator.GetPendingRecord(_testId);
    }

    /// <summary>
    /// 质量选择项包装类
    /// </summary>
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
