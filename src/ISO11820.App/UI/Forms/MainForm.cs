using ISO11820.App.App;
using ISO11820.App.UI.Common;
using ISO11820.App.UI.Dialogs;
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

    // --- Status strip (top) ---
    private readonly Label _stateLabel;
    private readonly Label _elapsedLabel;
    private readonly Label _operatorLabel;

    // --- Temperature displays (left) ---
    private readonly Label _furnace1Value;
    private readonly Label _furnace2Value;
    private readonly Label _surfaceValue;
    private readonly Label _centerValue;
    private readonly Label _calibrationValue;

    // --- Chart placeholder (center) ---
    private readonly Panel _chartPlaceholder;

    // --- Button group (right) ---
    private readonly Button _newTestButton;
    private readonly Button _startHeatingButton;
    private readonly Button _stopHeatingButton;
    private readonly Button _startRecordingButton;
    private readonly Button _stopRecordingButton;
    private readonly Button _parameterSettingsButton;

    // --- System messages (bottom) ---
    private readonly ListBox _messageListBox;

    public MainForm(Iso11820AppContext appContext)
    {
        _appContext = appContext;

        Text = "ISO 11820 仿真系统";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei", 10F);

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
        _messageListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 9F),
            BorderStyle = BorderStyle.None,
        };
        messagePanel.Controls.Add(_messageListBox);
        messagePanel.Controls.Add(messageHeader);

        // --- 2. Status strip (DockStyle.Top) ---
        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(235, 235, 235),
        };
        _stateLabel = new Label
        {
            Text = "当前状态：空闲",
            Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
            Location = new Point(12, 10),
            Size = new Size(200, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _elapsedLabel = new Label
        {
            Text = "计时：0 s",
            Font = new Font("Microsoft YaHei", 11F),
            Location = new Point(220, 10),
            Size = new Size(160, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _operatorLabel = new Label
        {
            Text = "操作员：---",
            Font = new Font("Microsoft YaHei", 10F),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(860, 10),
            Size = new Size(400, 28),
            TextAlign = ContentAlignment.MiddleRight,
        };
        statusPanel.Controls.AddRange(new Control[] { _stateLabel, _elapsedLabel, _operatorLabel });

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

        _newTestButton.Click += OnNewTestClick;
        _startHeatingButton.Click += OnStartHeatingClick;
        _stopHeatingButton.Click += OnStopHeatingClick;
        _startRecordingButton.Click += OnStartRecordingClick;
        _stopRecordingButton.Click += OnStopRecordingClick;
        _parameterSettingsButton.Click += OnParameterSettingsClick;

        buttonPanel.Controls.AddRange(new Control[]
        {
            _newTestButton,
            _startHeatingButton,
            _stopHeatingButton,
            _startRecordingButton,
            _stopRecordingButton,
            _parameterSettingsButton,
        });

        _buttonStateManager = new ButtonStateManager(
            _newTestButton,
            _startHeatingButton,
            _stopHeatingButton,
            _startRecordingButton,
            _stopRecordingButton,
            _parameterSettingsButton);

        // --- 4. Temperature panel (DockStyle.Left) ---
        var temperaturePanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
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
        _furnace1Value = CreateTempLabel("炉温 1：---.- °C", 32);
        _furnace2Value = CreateTempLabel("炉温 2：---.- °C", 80);
        _surfaceValue = CreateTempLabel("表面温：---.- °C", 128);
        _centerValue = CreateTempLabel("中心温：---.- °C", 176);
        _calibrationValue = CreateTempLabel("校准温：---.- °C", 224);
        temperaturePanel.Controls.AddRange(new Control[]
        {
            tempHeader,
            _furnace1Value,
            _furnace2Value,
            _surfaceValue,
            _centerValue,
            _calibrationValue,
        });

        // --- 5. Chart placeholder (DockStyle.Fill) ---
        _chartPlaceholder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        var chartLabel = new Label
        {
            Text = "图表区域（待接入）",
            Font = new Font("Microsoft YaHei", 14F),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
        };
        _chartPlaceholder.Controls.Add(chartLabel);

        // --- Add controls in Dock order ---
        Controls.Add(messagePanel);       // Bottom
        Controls.Add(statusPanel);        // Top
        Controls.Add(buttonPanel);        // Right
        Controls.Add(temperaturePanel);   // Left
        Controls.Add(_chartPlaceholder);  // Fill

        // --- Wire events ---
        Load += OnLoad;
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
            Font = new Font("Consolas", 12F),
            ForeColor = Color.FromArgb(0, 255, 128),
            Location = new Point(10, top),
            Size = new Size(200, 40),
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

        // Now subscribe to runtime broadcasts
        _appContext.TestController.DataBroadcast += OnDataBroadcast;
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
        // --- Status ---
        _stateLabel.Text = $"当前状态：{StateText(snapshot.State)}";
        _elapsedLabel.Text = $"计时：{snapshot.ElapsedSeconds} s";

        // --- Temperatures ---
        _furnace1Value.Text = $"炉温 1：{snapshot.Temperatures.Furnace1:F1} °C";
        _furnace2Value.Text = $"炉温 2：{snapshot.Temperatures.Furnace2:F1} °C";
        _surfaceValue.Text = $"表面温：{snapshot.Temperatures.Surface:F1} °C";
        _centerValue.Text = $"中心温：{snapshot.Temperatures.Center:F1} °C";
        _calibrationValue.Text = $"校准温：{snapshot.Temperatures.Calibration:F1} °C";

        // --- Button state matrix ---
        _buttonStateManager.Update(snapshot.State);

        // --- System messages ---
        foreach (var msg in snapshot.Messages)
        {
            _messageListBox.Items.Add($"[{msg.Time}] {msg.Message}");
        }

        // Auto-scroll to latest message
        if (_messageListBox.Items.Count > 0)
        {
            _messageListBox.TopIndex = _messageListBox.Items.Count - 1;
        }
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
        // If currently in Complete state, reset first
        _appContext.TestExecution.PrepareNewTest(_appContext.TestController);

        using var dialog = new NewTestDialog(_loginResult?.OperatorName ?? "");
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            // Placeholder: actual test creation will be wired when
            // persistence layer provides the save API.
        }
    }

    private void OnStartHeatingClick(object? sender, EventArgs e)
    {
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
        // Placeholder — will be expanded when parameter configuration
        // is added by the appropriate owner.
        MessageBox.Show("参数设置功能待接入。", "提示",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
