using ISO11820.App.App;
using ISO11820.App.Features.Calibration;
using ISO11820.App.Infrastructure.Persistence.Models;
using ISO11820.App.UI.Dialogs;
using System.Text.Json;

namespace ISO11820.App.UI.Panels;

public sealed class CalibrationPanel : UserControl
{
    private readonly Iso11820AppContext _appContext;

    // Session state
    private readonly List<CalibrationPoint> _sessionPoints = new();
    private double _currentTcal;

    // Top: TCal display
    private Label _tcalLabel = null!;

    // Session section
    private Panel _sessionPanel = null!;
    private TextBox _refTempInput = null!;
    private Button _recordPointButton = null!;
    private DataGridView _sessionGrid = null!;
    private Button _saveSessionButton = null!;
    private Button _resetSessionButton = null!;

    // History section
    private Panel _historyPanel = null!;
    private DataGridView _recordsGrid = null!;
    private Button _refreshButton = null!;

    public CalibrationPanel(Iso11820AppContext appContext)
    {
        _appContext = appContext;
        InitializeComponents();
        LoadRecords();
    }

    public void UpdateTcal(double value)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateTcal(value));
            return;
        }

        _currentTcal = value;
        _tcalLabel.Text = $"校准温度 TCal：{value:F1} °C";
    }

    private void InitializeComponents()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        InitializeTcalLabel();
        InitializeSessionPanel();
        InitializeHistoryPanel();

        Controls.Add(_historyPanel);
        Controls.Add(_sessionPanel);
        Controls.Add(_tcalLabel);

        ResumeLayout(false);
    }

    private void InitializeTcalLabel()
    {
        _tcalLabel = new Label
        {
            Text = "校准温度 TCal：---.- °C",
            Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };
    }

    private void InitializeSessionPanel()
    {
        _sessionPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 280,
            Padding = new Padding(0)
        };

        // Header
        var sessionHeader = new Label
        {
            Text = "当前校准会话",
            Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Input panel
        var sessionInputPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0),
            AutoSize = false
        };

        sessionInputPanel.Controls.Add(new Label
        {
            Text = "标准温度(°C)：",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 6, 0)
        });

        _refTempInput = new TextBox
        {
            Width = 90,
            Margin = new Padding(0, 4, 10, 0)
        };
        _refTempInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                OnRecordCalibrationPoint(null, EventArgs.Empty);
        };
        sessionInputPanel.Controls.Add(_refTempInput);

        _recordPointButton = new Button
        {
            Text = "记录此校准点",
            Width = 140,
            Height = 30,
            Margin = new Padding(0, 2, 0, 0)
        };
        _recordPointButton.Click += OnRecordCalibrationPoint;
        sessionInputPanel.Controls.Add(_recordPointButton);

        // Session grid
        _sessionGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D,
            Font = new Font("Microsoft YaHei UI", 9.5f)
        };

        _sessionGrid.Columns.Add("Index", "#");
        _sessionGrid.Columns["Index"].Width = 40;
        _sessionGrid.Columns["Index"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _sessionGrid.Columns.Add("Ref", "标准温度(°C)");
        _sessionGrid.Columns.Add("Measured", "实测温度(°C)");
        _sessionGrid.Columns.Add("Deviation", "偏差(°C)");
        _sessionGrid.Columns.Add("Time", "时间");

        // Button panel
        var sessionButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0),
            AutoSize = false
        };

        _saveSessionButton = new Button
        {
            Text = "保存校准记录",
            Width = 140,
            Height = 30,
            Margin = new Padding(0, 0, 12, 0)
        };
        _saveSessionButton.Click += OnSaveSession;
        sessionButtonPanel.Controls.Add(_saveSessionButton);

        _resetSessionButton = new Button
        {
            Text = "重置",
            Width = 80,
            Height = 30
        };
        _resetSessionButton.Click += OnResetSession;
        sessionButtonPanel.Controls.Add(_resetSessionButton);

        _sessionPanel.Controls.Add(_sessionGrid);
        _sessionPanel.Controls.Add(sessionButtonPanel);
        _sessionPanel.Controls.Add(sessionInputPanel);
        _sessionPanel.Controls.Add(sessionHeader);
    }

    private void InitializeHistoryPanel()
    {
        _historyPanel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var historyHeader = new Label
        {
            Text = "历史校准记录",
            Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _recordsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D,
            Font = new Font("Microsoft YaHei UI", 9.5f)
        };

        _recordsGrid.Columns.Add("CalibrationDate", "校准日期");
        _recordsGrid.Columns.Add("Technician", "操作员");
        _recordsGrid.Columns.Add("ResultJson", "校准结果");
        _recordsGrid.Columns.Add("Notes", "备注");
        _recordsGrid.CellClick += OnHistoryGridCellClick;

        var historyToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0),
            AutoSize = false
        };

        _refreshButton = new Button
        {
            Text = "刷新",
            Width = 80,
            Height = 30
        };
        _refreshButton.Click += (_, _) => LoadRecords();
        historyToolbar.Controls.Add(_refreshButton);

        _historyPanel.Controls.Add(_recordsGrid);
        _historyPanel.Controls.Add(historyToolbar);
        _historyPanel.Controls.Add(historyHeader);
    }

    private void OnRecordCalibrationPoint(object? sender, EventArgs e)
    {
        if (!double.TryParse(_refTempInput.Text.Trim(), out var refTemp))
        {
            MessageBox.Show("请输入有效的标准温度数值。", "输入错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _refTempInput.Focus();
            return;
        }

        var point = new CalibrationPoint
        {
            Ref = refTemp,
            Measured = _currentTcal,
            Deviation = _currentTcal - refTemp,
            Time = DateTime.Now.ToString("HH:mm:ss")
        };

        _sessionPoints.Add(point);
        RefreshSessionGrid();
        _refTempInput.Clear();
        _refTempInput.Focus();
    }

    private void OnSaveSession(object? sender, EventArgs e)
    {
        if (_sessionPoints.Count == 0)
        {
            MessageBox.Show("当前会话没有校准点，请先记录校准数据。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(_sessionPoints);
            var record = new CalibrationRecord
            {
                SensorId = 1,
                CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ResultJson = json,
                Technician = Environment.UserName,
                Notes = $"多点校准，共 {_sessionPoints.Count} 个点"
            };

            _appContext.Calibration.SaveRecord(record);
            _sessionPoints.Clear();
            RefreshSessionGrid();
            LoadRecords();

            MessageBox.Show("校准记录已保存。", "成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存校准记录失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnResetSession(object? sender, EventArgs e)
    {
        if (_sessionPoints.Count > 0)
        {
            var result = MessageBox.Show("确定要清除当前会话的所有校准点吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
        }

        _sessionPoints.Clear();
        RefreshSessionGrid();
        _refTempInput.Clear();
    }

    private void OnHistoryGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _recordsGrid.Rows.Count)
            return;

        var row = _recordsGrid.Rows[e.RowIndex];
        var date = row.Cells["CalibrationDate"].Value?.ToString() ?? "";
        var technician = row.Cells["Technician"].Value?.ToString();
        var notes = row.Cells["Notes"].Value?.ToString();
        var resultJson = row.Cells["ResultJson"].Value?.ToString();

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            MessageBox.Show("该记录没有校准数据。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        List<CalibrationPoint>? points = null;
        try
        {
            points = JsonSerializer.Deserialize<List<CalibrationPoint>>(resultJson);
        }
        catch
        {
            // Not multi-point JSON; show raw
        }

        if (points is { Count: > 0 })
        {
            using var dialog = new CalibrationDetailDialog(date, technician, notes, points);
            dialog.ShowDialog(this);
        }
        else
        {
            MessageBox.Show($"校准数据：\n{resultJson}", "校准记录详情",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void RefreshSessionGrid()
    {
        _sessionGrid.SuspendLayout();
        _sessionGrid.Rows.Clear();
        for (var i = 0; i < _sessionPoints.Count; i++)
        {
            var p = _sessionPoints[i];
            _sessionGrid.Rows.Add(
                i + 1,
                $"{p.Ref:F1}",
                $"{p.Measured:F1}",
                $"{p.Deviation:+#0.0;-#0.0;0.0}",
                p.Time);
        }
        _sessionGrid.ResumeLayout();
    }

    private void LoadRecords()
    {
        try
        {
            var records = _appContext.Calibration.QueryRecords();

            _recordsGrid.SuspendLayout();
            _recordsGrid.Rows.Clear();
            foreach (var record in records)
            {
                var summary = GetSummary(record.ResultJson);
                _recordsGrid.Rows.Add(
                    record.CalibrationDate,
                    record.Technician ?? "",
                    summary,
                    record.Notes ?? "");
            }
            _recordsGrid.ResumeLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载校准记录失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string GetSummary(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
            return "—";

        try
        {
            var points = JsonSerializer.Deserialize<List<CalibrationPoint>>(resultJson);
            if (points is { Count: > 0 })
                return $"{points.Count} 个校准点";
        }
        catch
        {
            // Fall back to showing the raw JSON (truncated)
        }

        return resultJson.Length > 50 ? resultJson[..50] + "…" : resultJson;
    }
}