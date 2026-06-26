namespace ISO11820.App.UI.Dialogs;

/// <summary>
/// New-test dialog shell — collects test metadata before a session starts.
/// Validation and persistence are handled by <c>Features/TestExecution</c>
/// and <c>Features/TestRecord</c>; this form only gathers input and
/// returns a <see cref="TestCreateInfo"/> on OK.
/// No SQL, no file-path logic.
/// </summary>
public sealed class NewTestDialog : Form
{
    private readonly TextBox _testNumberTextBox;
    private readonly ComboBox _testTypeCombo;
    private readonly TextBox _operatorTextBox;
    private readonly TextBox _notesTextBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    /// <summary>
    /// Set after the user clicks OK (<see cref="DialogResult"/> == <see cref="DialogResult.OK"/>).
    /// </summary>
    public TestCreateInfo? TestInfo { get; private set; }

    public NewTestDialog(string operatorName)
    {
        Text = "新建试验";
        Size = new Size(480, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei", 10F);

        // --- Test number ---
        var testNumberLabel = new Label
        {
            Text = "试验编号：",
            Location = new Point(30, 25),
            Size = new Size(100, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _testNumberTextBox = new TextBox
        {
            Location = new Point(140, 23),
            Size = new Size(280, 28),
        };

        // --- Test type ---
        var testTypeLabel = new Label
        {
            Text = "试验类型：",
            Location = new Point(30, 70),
            Size = new Size(100, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _testTypeCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(140, 68),
            Size = new Size(280, 28),
        };
        _testTypeCombo.Items.AddRange(new object[] { "常规试验", "校准试验", "比对试验" });
        _testTypeCombo.SelectedIndex = 0;

        // --- Operator ---
        var operatorLabel = new Label
        {
            Text = "操作员：",
            Location = new Point(30, 115),
            Size = new Size(100, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _operatorTextBox = new TextBox
        {
            Location = new Point(140, 113),
            Size = new Size(280, 28),
            Text = operatorName,
        };

        // --- Notes ---
        var notesLabel = new Label
        {
            Text = "备注：",
            Location = new Point(30, 160),
            Size = new Size(100, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _notesTextBox = new TextBox
        {
            Location = new Point(140, 158),
            Size = new Size(280, 80),
            Multiline = true,
        };

        // --- Buttons ---
        _okButton = new Button
        {
            Text = "确定",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(180, 270),
            Size = new Size(95, 36),
        };
        _okButton.Click += OnOkClick;

        _cancelButton = new Button
        {
            Text = "取消",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(295, 270),
            Size = new Size(95, 36),
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            testNumberLabel, _testNumberTextBox,
            testTypeLabel, _testTypeCombo,
            operatorLabel, _operatorTextBox,
            notesLabel, _notesTextBox,
            _okButton, _cancelButton,
        });

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
        ActiveControl = _testNumberTextBox;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        var number = _testNumberTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(number))
        {
            MessageBox.Show("请输入试验编号。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _testNumberTextBox.Focus();
            return;
        }

        TestInfo = new TestCreateInfo(
            number,
            _testTypeCombo.Text,
            _operatorTextBox.Text.Trim(),
            _notesTextBox.Text.Trim());

        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// Carries test creation input from <see cref="NewTestDialog"/> to coordinators.
/// </summary>
public sealed record TestCreateInfo(
    string TestNumber,
    string TestType,
    string OperatorName,
    string Notes);
