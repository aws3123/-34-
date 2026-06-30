using ISO11820.App.Features.Auth;
using ISO11820.App.UI.Common;

namespace ISO11820.App.UI.Forms;

/// <summary>
/// 登录窗体 — 角色选择（RadioButton）+ 密码输入 + 数据库验证
/// </summary>
public sealed class LoginForm : Form
{
    private readonly AuthCoordinator _auth;
    private static LoginForm? _currentInstance;

    private readonly RadioButton _adminRadio;
    private readonly RadioButton _experimenterRadio;
    private readonly TextBox _passwordTextBox;
    private readonly Button _loginButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    // --- Test signal support ---
    private const int WM_TEST_LOGIN = 0x8065; // WM_APP + 101
    private static System.Threading.Thread? _loginSignalThread;
    private static volatile bool _loginSignalRunning;

    public LoginResult? LoginResult { get; private set; }

    public LoginForm(AuthCoordinator auth)
    {
        _auth = auth;
        _currentInstance = this;
        StartLoginSignalWatcher();

        Text = "ISO 11820 — 登录";
        Size = new Size(520, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // --- Title ---
        var titleLabel = new Label
        {
            Text = "ISO 11820 仿真系统",
            Font = new Font("Microsoft YaHei", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80,
        };

        // --- Form body using TableLayoutPanel ---
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(40, 10, 40, 20),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        // --- Role row (RadioButtons) ---
        var roleLabel = new Label
        {
            Text = "角色：",
            Font = new Font("Microsoft YaHei", 12F),
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 0, 8, 0),
        };

        var rolePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 8),
        };

        _adminRadio = new RadioButton
        {
            Text = "管理员",
            Font = new Font("Microsoft YaHei", 12F),
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0),
            Checked = true,
        };

        _experimenterRadio = new RadioButton
        {
            Text = "试验员",
            Font = new Font("Microsoft YaHei", 12F),
            AutoSize = true,
        };

        rolePanel.Controls.Add(_adminRadio);
        rolePanel.Controls.Add(_experimenterRadio);

        // --- Password row ---
        var passwordLabel = new Label
        {
            Text = "密码：",
            Font = new Font("Microsoft YaHei", 12F),
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 0, 8, 0),
        };

        _passwordTextBox = new TextBox
        {
            Font = new Font("Microsoft YaHei", 12F),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 8),
            UseSystemPasswordChar = true,
        };

        // --- Error label ---
        _errorLabel = new Label
        {
            Text = "",
            ForeColor = Color.Red,
            Font = new Font("Microsoft YaHei", 9F),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
        };

        // --- Buttons row ---
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
        };

        _loginButton = new Button
        {
            Text = "登录",
            Font = new Font("Microsoft YaHei", 11F),
            Size = new Size(120, 42),
            Margin = new Padding(0, 0, 20, 0),
        };
        _loginButton.Click += OnLoginClick;

        _cancelButton = new Button
        {
            Text = "取消",
            Font = new Font("Microsoft YaHei", 11F),
            Size = new Size(120, 42),
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonPanel.Controls.AddRange(new Control[] { _loginButton, _cancelButton });

        // Add controls to table
        table.Controls.Add(roleLabel, 0, 1);
        table.Controls.Add(rolePanel, 1, 1);
        table.Controls.Add(passwordLabel, 0, 2);
        table.Controls.Add(_passwordTextBox, 1, 2);
        table.Controls.Add(_errorLabel, 1, 3);
        table.Controls.Add(buttonPanel, 0, 4);
        table.SetColumnSpan(buttonPanel, 2);

        // --- Assemble form ---
        Controls.Add(table);
        Controls.Add(titleLabel);

        AcceptButton = _loginButton;
        CancelButton = _cancelButton;
        ActiveControl = _passwordTextBox;
    }

    /// <summary>
    /// 获取当前选中的角色对应的数据库用户名
    /// </summary>
    private string GetSelectedUsername()
    {
        return _adminRadio.Checked ? "admin" : "experimenter";
    }

    /// <summary>
    /// 获取当前选中的角色显示名称
    /// </summary>
    private string GetSelectedRoleName()
    {
        return _adminRadio.Checked ? "管理员" : "试验员";
    }

    private void OnLoginClick(object? sender, EventArgs e)
    {
        var username = GetSelectedUsername();
        var password = _passwordTextBox.Text;

        var (success, errorMessage, role) = _auth.TryLogin(username, password);

        if (success)
        {
            // LoginResult 使用数据库用户名（admin/experimenter）
            LoginResult = new LoginResult(username, username);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _errorLabel.Text = errorMessage ?? "登录失败";
        }
    }

    // ═══════════════════════════════════════════════════
    //  TEST HOOK: 通过 SendMessage + WM_TEST_LOGIN 触发登录
    // ═══════════════════════════════════════════════════

    private static void StartLoginSignalWatcher()
    {
        if (_loginSignalRunning) return;
        var signalDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_signals");
        System.IO.Directory.CreateDirectory(signalDir);

        _loginSignalRunning = true;
        _loginSignalThread = new System.Threading.Thread(() =>
        {
            while (_loginSignalRunning)
            {
                try
                {
                    var files = Directory.GetFiles(signalDir, "click_login.signal");
                    foreach (var f in files)
                    {
                        bool deleted = false;
                        for (int retry = 0; retry < 5; retry++)
                        {
                            try { System.IO.File.Delete(f); deleted = true; break; }
                            catch { System.Threading.Thread.Sleep(50); }
                        }
                        if (!deleted) continue;

                        var inst = _currentInstance;
                        if (inst != null && inst.IsHandleCreated)
                        {
                            // wParam=1 → admin, wParam=2 → experimenter
                            // 通过 SendMessage 直接在 UI 线程执行
                            SendMessage(inst.Handle, WM_TEST_LOGIN, 1, 0);
                        }
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(200);
            }
        })
        { IsBackground = true, Name = "LoginSignalPoller" };
        _loginSignalThread.Start();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_TEST_LOGIN)
        {
            // 在 UI 线程上直接执行登录
            _adminRadio.Checked = true;
            _passwordTextBox.Text = "123456";
            OnLoginClick(this, EventArgs.Empty);
            return;
        }
        base.WndProc(ref m);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, uint Msg, int wParam, int lParam);

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _loginSignalRunning = false;
        _currentInstance = null;
        base.OnFormClosed(e);
    }
}
