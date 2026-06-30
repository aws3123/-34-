using ISO11820.App.App;
using ISO11820.App.Config;
using ISO11820.App.Infrastructure.Persistence.Models;
using ISO11820.App.UI.Chart;
using ISO11820.App.UI.Common;
using ISO11820.App.UI.Dialogs;
using ISO11820.App.UI.Panels;
using ISO11820.App.Shared.Events;
using ISO11820.App.Shared.Models;
using ISO11820.Core.Enums;
using ISO11820.Core.Models;

namespace ISO11820.App.UI.Forms;

/// <summary>
/// Main application form — displays runtime state, temperatures, buttons,
/// and system messages. All UI updates from background threads go through
/// <see cref="InvokeRequired"/>/<see cref="Invoke"/> to prevent cross-thread access.
/// No SQL, no state-machine core logic, no file-path manipulation.
/// Login is handled internally via <see cref="LoginForm"/> dialog on load.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Iso11820AppContext _appContext;
    private readonly ButtonStateManager _buttonStateManager;
    private LoginResult? _loginResult;
    private TestCreateInfo? _currentTestInfo;
    private bool _csvDataSaved;

    // --- Tab 页结构 ---
    private readonly TabControl _tabControl;
    private readonly TabPage _mainTabPage;
    private readonly TabPage _recordQueryTabPage;
    private readonly TabPage _calibrationTabPage;

    // --- Status strip (top) ---
    private readonly Label _stateLabel;
    private readonly Label _elapsedLabel;
    private readonly Label _operatorLabel;
    private readonly Label _driftLabel;
    private readonly Label _sampleIdLabel;

    // --- Temperature displays (left) ---
    private readonly Label _furnace1Value;
    private readonly Label _furnace2Value;
    private readonly Label _surfaceValue;
    private readonly Label _centerValue;
    private readonly Label _calibrationValue;
    private readonly CalibrationPanel _calibrationPanel;

    // --- Chart (center) ---
    private readonly TemperatureChartPanel _chartPanel;

    // --- Button group (right) ---
    private readonly Button _newTestButton;
    private readonly Button _startHeatingButton;
    private readonly Button _stopHeatingButton;
    private readonly Button _startRecordingButton;
    private readonly Button _stopRecordingButton;
    private readonly Button _parameterSettingsButton;
    private readonly Button _testRecordButton;

    // --- System messages (bottom) ---
    private readonly RichTextBox _messageListBox;

    // --- Record query tab ---
    private readonly DataGridView _recordGrid;
    private readonly Button _refreshRecordButton;

    // --- Record query filters ---
    private readonly DateTimePicker _dateFromPicker;
    private readonly DateTimePicker _dateToPicker;
    private readonly TextBox _sampleIdFilterBox;
    private readonly ComboBox _operatorFilterCombo;
    private readonly Button _searchButton;
    private readonly Button _exportButton;
    private List<TestMaster> _lastQueryResults = new();

    public MainForm(Iso11820AppContext appContext)
    {
        _appContext = appContext;

        Text = "ISO 11820 仿真系统";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei", 10F);

        // ================================================================
        //  TEST HOOK: 监控文件信号，供 UI 自动化测试使用
        // ================================================================
        StartTestFileWatcher();

        // ================================================================
        // Build layout — Dock order matters:
        //   Bottom → Top → Right → Left → Fill
        // Controls added later dock into remaining space.
        // ================================================================

        // --- 1. Message panel (DockStyle.Bottom) ---
        var messagePanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 140,
        };
        var messageHeader = new Label
        {
            Text = "系统消息",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(220, 220, 220),
        };
        _messageListBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 9F),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            BackColor = Color.White,
        };
        messagePanel.Controls.Add(_messageListBox);
        messagePanel.Controls.Add(messageHeader);

        // --- 2. Status strip (DockStyle.Top) ---
        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(235, 235, 235),
        };
        _stateLabel = new Label
        {
            Text = "当前状态：空闲",
            Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
            Location = new Point(12, 12),
            AutoSize = true,
            MaximumSize = new Size(300, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _elapsedLabel = new Label
        {
            Text = "计时：0 s",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(320, 12),
            AutoSize = true,
            MaximumSize = new Size(150, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _driftLabel = new Label
        {
            Text = "温漂：0.00 °C/10min",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(480, 12),
            AutoSize = true,
            MaximumSize = new Size(220, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DarkBlue,
        };
        _sampleIdLabel = new Label
        {
            Text = "样品：---",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(710, 12),
            AutoSize = true,
            MaximumSize = new Size(260, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _operatorLabel = new Label
        {
            Text = "操作员：---",
            Font = new Font("Microsoft YaHei", 10F),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(900, 12),
            Size = new Size(360, 32),
            TextAlign = ContentAlignment.MiddleRight,
        };
        statusPanel.Controls.AddRange(new Control[] { _stateLabel, _elapsedLabel, _driftLabel, _sampleIdLabel, _operatorLabel });

        // --- 3. Button panel (DockStyle.Right) ---
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 160,
            BackColor = Color.FromArgb(245, 245, 245),
        };
        _newTestButton = CreateButton("新建试验", 20);
        _startHeatingButton = CreateButton("开始升温", 70);
        _stopHeatingButton = CreateButton("停止升温", 120);
        _startRecordingButton = CreateButton("开始记录", 170);
        _stopRecordingButton = CreateButton("停止记录", 220);
        _parameterSettingsButton = CreateButton("参数设置", 270);
        _testRecordButton = CreateButton("试验记录", 320);

        _newTestButton.Click += OnNewTestClick;
        _startHeatingButton.Click += OnStartHeatingClick;
        _stopHeatingButton.Click += OnStopHeatingClick;
        _startRecordingButton.Click += OnStartRecordingClick;
        _stopRecordingButton.Click += OnStopRecordingClick;
        _parameterSettingsButton.Click += OnParameterSettingsClick;
        _testRecordButton.Click += OnTestRecordClick;

        buttonPanel.Controls.AddRange(new Control[]
        {
            _newTestButton,
            _startHeatingButton,
            _stopHeatingButton,
            _startRecordingButton,
            _stopRecordingButton,
            _parameterSettingsButton,
            _testRecordButton,
        });

        _buttonStateManager = new ButtonStateManager(
            _newTestButton,
            _startHeatingButton,
            _stopHeatingButton,
            _startRecordingButton,
            _stopRecordingButton,
            _parameterSettingsButton,
            _testRecordButton);

        // --- 4. Temperature panel (DockStyle.Left) ---
        var temperaturePanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 280,
            BackColor = Color.FromArgb(30, 30, 30),
        };
        var tempHeader = new Label
        {
            Text = "温度通道",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(200, 200, 200),
        };
        _furnace1Value = CreateTempLabel("炉温 1：---.- °C", 36);
        _furnace2Value = CreateTempLabel("炉温 2：---.- °C", 90);
        _surfaceValue = CreateTempLabel("表面温度：---.- °C", 144);
        _centerValue = CreateTempLabel("中心温度：---.- °C", 198);
        _calibrationValue = CreateTempLabel("校准温度：---.- °C", 252);
        temperaturePanel.Controls.AddRange(new Control[]
        {
            tempHeader,
            _furnace1Value,
            _furnace2Value,
            _surfaceValue,
            _centerValue,
            _calibrationValue,
        });

        // --- 5. Chart (DockStyle.Fill) ---
        _chartPanel = new TemperatureChartPanel();

        // ================================================================
        // TabControl 页结构
        // ================================================================

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10F),
        };

        // Tab 1: 主操作界面
        _mainTabPage = new TabPage("主操作界面")
        {
            Padding = new Padding(0),
        };
        // Fill 控件最先添加（z-order 最低），确保它在边缘 Dock 之后布局，避免覆盖状态栏
        _mainTabPage.Controls.Add(_chartPanel.View);
        _mainTabPage.Controls.Add(messagePanel);
        _mainTabPage.Controls.Add(statusPanel);
        _mainTabPage.Controls.Add(buttonPanel);
        _mainTabPage.Controls.Add(temperaturePanel);

        // Tab 2: 记录查询
        _recordQueryTabPage = new TabPage("记录查询")
        {
            Padding = new Padding(10),
        };

        // 工具栏（顶部按钮区）
        var recordToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
        };
        _refreshRecordButton = new Button
        {
            Text = "刷新",
            Font = new Font("Microsoft YaHei", 10F),
            Size = new Size(80, 32),
            Location = new Point(0, 6),
        };
        _refreshRecordButton.Click += (_, _) => RefreshRecordGrid();

        var recordTitle = new Label
        {
            Text = "历史试验记录",
            Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
            Location = new Point(100, 8),
            AutoSize = true,
        };
        recordToolbar.Controls.AddRange(new Control[] { _refreshRecordButton, recordTitle });

        // 筛选面板
        var filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.FromArgb(248, 248, 248),
        };

        var dateFromLabel = new Label
        {
            Text = "开始日期：",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(4, 10),
            AutoSize = true,
        };
        _dateFromPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(80, 8),
            Size = new Size(120, 24),
            ShowCheckBox = true,
            Checked = false,
        };
        var dateToLabel = new Label
        {
            Text = "结束日期：",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(214, 10),
            AutoSize = true,
        };
        _dateToPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(290, 8),
            Size = new Size(120, 24),
            ShowCheckBox = true,
            Checked = false,
        };

        var sampleLabel = new Label
        {
            Text = "样品编号：",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(4, 44),
            AutoSize = true,
        };
        _sampleIdFilterBox = new TextBox
        {
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(80, 42),
            Size = new Size(120, 24),
        };
        var operatorLabel = new Label
        {
            Text = "操作员：",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(214, 44),
            AutoSize = true,
        };
        _operatorFilterCombo = new ComboBox
        {
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(290, 42),
            Size = new Size(120, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _searchButton = new Button
        {
            Text = "查询",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(430, 40),
            Size = new Size(70, 28),
        };
        _searchButton.Click += OnSearchClick;
        _exportButton = new Button
        {
            Text = "导出 Excel",
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(510, 40),
            Size = new Size(90, 28),
        };
        _exportButton.Click += OnExportClick;

        filterPanel.Controls.AddRange(new Control[]
        {
            dateFromLabel, _dateFromPicker, dateToLabel, _dateToPicker,
            sampleLabel, _sampleIdFilterBox, operatorLabel, _operatorFilterCombo,
            _searchButton, _exportButton,
        });

        // 数据表格
        _recordGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei", 9F),
        };
        _recordGrid.Columns.Add("productid", "样品编号");
        _recordGrid.Columns.Add("testid", "试验标识");
        _recordGrid.Columns.Add("sample_name", "样品名称");
        _recordGrid.Columns.Add("operator", "操作员");
        _recordGrid.Columns.Add("testdate", "试验日期");

        _recordGrid.CellDoubleClick += OnRecordGridDoubleClick;

        _recordQueryTabPage.Controls.Add(_recordGrid);
        _recordQueryTabPage.Controls.Add(filterPanel);
        _recordQueryTabPage.Controls.Add(recordToolbar);

        // Tab 3: 设备校准
        _calibrationTabPage = new TabPage("设备校准")
        {
            Padding = new Padding(20),
        };
        _calibrationPanel = new CalibrationPanel(_appContext);
        _calibrationTabPage.Controls.Add(_calibrationPanel);

        _tabControl.TabPages.Add(_mainTabPage);
        _tabControl.TabPages.Add(_recordQueryTabPage);
        _tabControl.TabPages.Add(_calibrationTabPage);

        // 当切换到记录查询 tab 时自动刷新数据
        _tabControl.SelectedIndexChanged += (_, _) =>
        {
            if (_tabControl.SelectedTab == _recordQueryTabPage)
            {
                RefreshRecordGrid();
            }
        };

        // --- 添加 TabControl 到主窗体 ---
        Controls.Add(_tabControl);

        // --- Wire events ---
        Load += OnLoad;
        Shown += OnShown;
        FormClosing += OnFormClosing;

        // --- Initial button state ---
        _buttonStateManager.Update(TestState.Idle);
    }

    // ================================================================
    // Control factory helpers
    // ================================================================

    private static Button CreateButton(string text, int top)
    {
        return new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(12, top),
            Size = new Size(136, 40),
            FlatStyle = FlatStyle.Standard,
        };
    }

    private static Label CreateTempLabel(string text, int top)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Microsoft YaHei", 11F),
            ForeColor = Color.FromArgb(0, 255, 128),
            Location = new Point(10, top),
            MinimumSize = new Size(0, 40),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(20, 20, 20),
        };
    }

    // ================================================================
    // Lifecycle — login happens here, not in Program.cs
    // ================================================================

    private void OnLoad(object? sender, EventArgs e)
    {
        // Show login dialog first — if cancelled or failed, close MainForm
        using var loginForm = new LoginForm(_appContext.Auth);
        if (loginForm.ShowDialog(this) != DialogResult.OK || loginForm.LoginResult is null)
        {
            Close();
            return;
        }

        _loginResult = loginForm.LoginResult;
        _operatorLabel.Text = $"操作员：{_loginResult.OperatorName}";

        // 加载操作员下拉列表
        _operatorFilterCombo.Items.Add("全部");
        foreach (var op in _appContext.History.QueryOperators())
        {
            _operatorFilterCombo.Items.Add(op.Username);
        }
        _operatorFilterCombo.SelectedIndex = 0;

        // Subscribe to data broadcasts (DAQ starts in OnShown after layout completes)
        _appContext.TestController.DataBroadcast += OnDataBroadcast;
    }

    private void OnShown(object? sender, EventArgs e)
    {
        _appContext.DaqWorker.Start();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _appContext.TestController.DataBroadcast -= OnDataBroadcast;
        _appContext.DaqWorker.Stop();
    }

    // ================================================================
    // DataBroadcast handler — must marshal to UI thread
    // ================================================================

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDataBroadcast(sender, e));
            return;
        }

        UpdateFromSnapshot(e.Snapshot);
    }

    private void UpdateFromSnapshot(RuntimeSnapshot snapshot)
    {
        // 诊断日志
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] UpdateFromSnapshot: state={snapshot.State}, elapsed={snapshot.ElapsedSeconds}s, F1={snapshot.Temperatures.Furnace1:F1}\n");
        }
        catch { }

        // --- Status ---
        _stateLabel.Text = $"当前状态：{StateText(snapshot.State)}";
        _elapsedLabel.Text = $"计时：{snapshot.ElapsedSeconds} s";

        // --- Temperature drift ---
        var drift = _appContext.TestController.GetTemperatureDrift();
        _driftLabel.Text = $"温漂：{drift * 600:F2} °C/10min";

        // --- Temperatures ---
        _furnace1Value.Text = $"炉温 1：{snapshot.Temperatures.Furnace1:F1} °C";
        _furnace2Value.Text = $"炉温 2：{snapshot.Temperatures.Furnace2:F1} °C";
        _surfaceValue.Text = $"表面温度：{snapshot.Temperatures.Surface:F1} °C";
        _centerValue.Text = $"中心温度：{snapshot.Temperatures.Center:F1} °C";
        _calibrationValue.Text = $"校准温度：{snapshot.Temperatures.Calibration:F1} °C";
        _calibrationPanel.UpdateTcal(snapshot.Temperatures.Calibration);

        // --- Button state matrix ---
        _buttonStateManager.Update(snapshot.State);

        // --- Save sensor data to CSV when test completes ---
        if (snapshot.State == TestState.Complete && !_csvDataSaved)
        {
            _csvDataSaved = true;
            SaveSensorDataToCsv();
        }

        // --- Chart ---
        if (snapshot.State != TestState.Idle)
        {
            _chartPanel.AppendSample(snapshot.ChartElapsedSeconds, snapshot.Temperatures);
        }

        // --- System messages (colored RichTextBox) ---
        foreach (var msg in snapshot.Messages)
        {
            var text = $"[{msg.Time}] {msg.Message}\n";
            if (msg.Message.Contains("终止") || msg.Message.Contains("自动结束"))
            {
                _messageListBox.SelectionColor = Color.DarkOrange;
            }
            else
            {
                _messageListBox.SelectionColor = Color.Black;
            }
            _messageListBox.AppendText(text);
        }

        // Auto-scroll
        _messageListBox.SelectionStart = _messageListBox.TextLength;
        _messageListBox.ScrollToCaret();
    }

    private static string StateText(TestState state)
    {
        return state switch
        {
            TestState.Idle => "空闲",
            TestState.Preparing => "升温中",
            TestState.Ready => "就绪",
            TestState.Recording => "记录中",
            TestState.Complete => "已完成",
            _ => state.ToString(),
        };
    }

    // ================================================================
    // Button click handlers — delegate to coordinator / controller
    // ================================================================

    private void OnNewTestClick(object? sender, EventArgs e)
    {
        // DIAGNOSTIC: 确认按钮处理器被调用
        try
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_newtest_called.txt"),
                $"OnNewTestClick called at {System.DateTime.Now:HH:mm:ss.fff}\n");
        }
        catch { }

        try
        {
            // If currently in Complete state, reset first
            _appContext.TestExecution.PrepareNewTest(_appContext.TestController);
            _chartPanel.Clear();

            using var dialog = new NewTestDialog(_loginResult?.OperatorName ?? "");
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _currentTestInfo = dialog.TestInfo;
                _sampleIdLabel.Text = $"样品：{dialog.TestInfo?.ProductId ?? "---"}";
                _csvDataSaved = false;
                _appContext.TestExecution.SaveTestToDb(dialog.TestInfo, _appContext.DbHelper);
                _appContext.TestExecution.SaveProductToDb(dialog.TestInfo, _appContext.DbHelper);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新建试验失败:\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnStartHeatingClick(object? sender, EventArgs e)
    {
        _chartPanel.Clear();
        _appContext.TestController.StartHeating();
    }

    private void OnStopHeatingClick(object? sender, EventArgs e)
    {
        _appContext.TestController.StopHeating();
    }

    private void OnStartRecordingClick(object? sender, EventArgs e)
    {
        _appContext.TestController.StartRecording();
    }

    private void OnStopRecordingClick(object? sender, EventArgs e)
    {
        _appContext.TestController.StopRecording();
    }

    private void OnParameterSettingsClick(object? sender, EventArgs e)
    {
        using var dialog = new ParameterSettingsDialog(_appContext.Settings.Simulation);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.UpdatedSettings != null)
        {
            _appContext.TestController.UpdateSimulationSettings(dialog.UpdatedSettings);
        }
    }

    private void OnTestRecordClick(object? sender, EventArgs e)
    {
        var productId = _currentTestInfo?.ProductId ?? "";
        var testId = _currentTestInfo?.TestId ?? "";

        using var dialog = new ExportDialog(
            _appContext.Export,
            productId,
            testId,
            () => ExportChartImage());
        dialog.ShowDialog(this);
    }

    /// <summary>
    /// 导出温度曲线图为图片，用于 Excel/PDF 报告
    /// </summary>
    public System.Drawing.Image? ExportChartImage(int width = 800, int height = 500)
    {
        return _chartPanel.ExportImage(width, height);
    }

    private void SaveSensorDataToCsv()
    {
        try
        {
            var productId = _currentTestInfo?.ProductId;
            var testId = _currentTestInfo?.TestId;
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(testId))
                return;

            var data = _appContext.TestController.SensorDataBuffer;
            _appContext.Export.SaveSensorDataToCsv(productId, testId, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveSensorDataToCsv failed: {ex.Message}");
        }
    }

    // ================================================================
    // Record Query — load from HistoryCoordinator
    // ================================================================

    /// <summary>
    /// 刷新记录查询表格。
    /// 当前从 HistoryCoordinator.QueryTestTypes() 加载数据并映射到表格列。
    /// 当 testmaster 表结构由其他 agent 完成调整后会更新映射。
    /// </summary>
    private void RefreshRecordGrid()
    {
        try
        {
            var productIdLike = string.IsNullOrWhiteSpace(_sampleIdFilterBox.Text)
                ? null : _sampleIdFilterBox.Text.Trim();
            var operatorName = _operatorFilterCombo.SelectedIndex > 0
                ? _operatorFilterCombo.SelectedItem?.ToString() : null;
            var dateFrom = _dateFromPicker.Checked
                ? _dateFromPicker.Value.ToString("yyyy-MM-dd") : null;
            var dateTo = _dateToPicker.Checked
                ? _dateToPicker.Value.ToString("yyyy-MM-dd") : null;

            _lastQueryResults = _appContext.History.QueryTests(
                productIdLike, operatorName, dateFrom, dateTo);

            _recordGrid.Rows.Clear();
            foreach (var r in _lastQueryResults)
            {
                _recordGrid.Rows.Add(
                    r.ProductId,
                    r.TestId,
                    r.SampleName ?? "",
                    r.Operator ?? "",
                    r.TestDate);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshRecordGrid error: {ex.Message}");
        }
    }

    private void OnSearchClick(object? sender, EventArgs e)
    {
        RefreshRecordGrid();
    }

    private void OnExportClick(object? sender, EventArgs e)
    {
        if (_lastQueryResults.Count == 0)
        {
            MessageBox.Show("没有可导出的数据。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var saveDialog = new SaveFileDialog
        {
            Title = "导出查询结果",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            FileName = $"查询结果_{DateTime.Now:yyyyMMdd}.xlsx",
        };

        if (saveDialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _appContext.History.ExportToExcel(_lastQueryResults, saveDialog.FileName);
                MessageBox.Show($"导出成功！\n\n文件路径：{saveDialog.FileName}",
                    "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnRecordGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _lastQueryResults.Count)
            return;

        var test = _lastQueryResults[e.RowIndex];
        using var dialog = new TestDetailDialog(test);
        dialog.ShowDialog(this);
    }

    // ================================================================
    //  TEST HOOK: 文件信号监听
    //  供 UI 自动化测试触发按钮操作（当物理点击/UIA 不可靠时使用）
    // ================================================================

    private const int WM_TEST_SIGNAL = 0x8064; // WM_APP + 100
    private System.Threading.Thread? _signalPollThread;
    private volatile bool _signalPollRunning;

    private void StartTestFileWatcher()
    {
        var signalDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_signals");
        System.IO.Directory.CreateDirectory(signalDir);
        var logPath = System.IO.Path.Combine(signalDir, "_signal_log.txt");
        try { System.IO.File.AppendAllText(logPath,
            $"[{DateTime.Now:HH:mm:ss.fff}] Watcher starting (PostMessage thread)\n"); } catch { }

        _signalPollRunning = true;
        _signalPollThread = new System.Threading.Thread(() =>
        {
            try { System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] Poll thread started\n"); } catch { }

            while (_signalPollRunning)
            {
                try
                {
                    var files = Directory.GetFiles(signalDir, "*.signal");
                    foreach (var f in files)
                    {
                        var signalName = System.IO.Path.GetFileNameWithoutExtension(f);
                        if (string.IsNullOrEmpty(signalName)) continue;

                        // 只处理 MainForm 负责的信号，其他信号留给 LoginForm 等处理
                        int signalCode = signalName switch
                        {
                            "click_newtest" => 1,
                            "click_start_heating" => 2,
                            "click_stop_heating" => 3,
                            "click_start_recording" => 4,
                            "click_stop_recording" => 5,
                            _ => 0
                        };
                        if (signalCode == 0) continue; // 不是我们的信号，不删除文件

                        // 删除信号文件（带重试）
                        bool deleted = false;
                        for (int retry = 0; retry < 5; retry++)
                        {
                            try { System.IO.File.Delete(f); deleted = true; break; }
                            catch { System.Threading.Thread.Sleep(50); }
                        }
                        if (!deleted) continue;
                        if (signalCode > 0 && IsHandleCreated)
                        {
                            // 使用 SendMessage 同步发送（直接调用 WndProc，不经过消息队列）
                            try
                            {
                                var result = SendMessage(Handle, WM_TEST_SIGNAL, signalCode, 0);
                                System.IO.File.AppendAllText(logPath,
                                    $"[{DateTime.Now:HH:mm:ss.fff}] SendMessage result={result}, handle=0x{Handle:X}\n");
                            }
                            catch (Exception sendEx)
                            {
                                System.IO.File.AppendAllText(logPath,
                                    $"[{DateTime.Now:HH:mm:ss.fff}] SendMessage EXCEPTION: {sendEx.GetType().Name}: {sendEx.Message}\n");
                            }
                        }
                        else
                        {
                            try { System.IO.File.AppendAllText(logPath,
                                $"[{DateTime.Now:HH:mm:ss.fff}] Skipped: signalCode={signalCode}, handleCreated={IsHandleCreated}\n"); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    try { System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] Poll thread ERROR: {ex.Message}\n"); } catch { }
                }
                System.Threading.Thread.Sleep(200);
            }
        })
        { IsBackground = true, Name = "TestSignalPoller" };
        _signalPollThread.Start();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_TEST_SIGNAL)
        {
            var logPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "iso11820_signals", "_signal_log.txt");
            try { System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] WndProc received code={m.WParam}\n"); } catch { }

            var signalName = (int)m.WParam switch
            {
                1 => "click_newtest",
                2 => "click_start_heating",
                3 => "click_stop_heating",
                4 => "click_start_recording",
                5 => "click_stop_recording",
                _ => ""
            };
            if (!string.IsNullOrEmpty(signalName))
            {
                HandleTestSignal(signalName);
            }
            return;
        }
        base.WndProc(ref m);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint Msg, int wParam, int lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, uint Msg, int wParam, int lParam);

    private void HandleTestSignal(string signalName)
    {
        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_signals", "_signal_log.txt");
        try { System.IO.File.AppendAllText(logPath,
            $"[{DateTime.Now:HH:mm:ss.fff}] HandleTestSignal: {signalName}, state={_appContext.TestController.CurrentState}\n"); } catch { }

        try
        {
            switch (signalName)
            {
                case "click_newtest":
                    OnNewTestClick(this, EventArgs.Empty);
                    break;
                case "click_start_heating":
                    OnStartHeatingClick(this, EventArgs.Empty);
                    break;
                case "click_stop_heating":
                    OnStopHeatingClick(this, EventArgs.Empty);
                    break;
                case "click_start_recording":
                    OnStartRecordingClick(this, EventArgs.Empty);
                    break;
                case "click_stop_recording":
                    OnStopRecordingClick(this, EventArgs.Empty);
                    break;
            }
        }
        catch (Exception ex)
        {
            try { System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] HandleTestSignal EXCEPTION: {ex.GetType().Name}: {ex.Message}\n"); } catch { }
        }

        try { System.IO.File.AppendAllText(logPath,
            $"[{DateTime.Now:HH:mm:ss.fff}] HandleTestSignal done, state={_appContext.TestController.CurrentState}\n"); } catch { }
    }
}
