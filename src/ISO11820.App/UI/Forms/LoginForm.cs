using ISO11820.App.Features.Auth;
using ISO11820.App.UI.Common;

namespace ISO11820.App.UI.Forms;

/// <summary>
/// Login form shell — collects role and password input.
/// Validation is deferred to <see cref="AuthCoordinator"/> which is
/// currently a shell placeholder. The error label is kept in the
/// form structure for future use when real validation is wired in.
/// No SQL, no DB dependency.
/// </summary>
public sealed class LoginForm : Form
{
    private readonly AuthCoordinator _auth;

    private readonly ComboBox _roleCombo;
    private readonly TextBox _passwordTextBox;
    private readonly Button _loginButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Set after a successful login (<see cref="DialogResult"/> == <see cref="DialogResult.OK"/>).
    /// </summary>
    public LoginResult? LoginResult { get; private set; }

    public LoginForm(AuthCoordinator auth)
    {
        _auth = auth;

        Text = "ISO 11820 — 登录";
        Size = new Size(420, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // --- Title ---
        var titleLabel = new Label
        {
            Text = "ISO 11820 仿真系统",
            Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 25),
            Size = new Size(ClientSize.Width, 40),
        };

        // --- Role ---
        var roleLabel = new Label
        {
            Text = "角色：",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(40, 90),
            Size = new Size(80, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _roleCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(130, 88),
            Size = new Size(230, 28),
        };
        _roleCombo.Items.AddRange(new object[] { "操作员", "管理员" });
        _roleCombo.SelectedIndex = 0;

        // --- Password ---
        var passwordLabel = new Label
        {
            Text = "密码：",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(40, 135),
            Size = new Size(80, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _passwordTextBox = new TextBox
        {
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(130, 133),
            Size = new Size(230, 28),
            UseSystemPasswordChar = true,
        };

        // --- Error label (placeholder in form structure, unused until real validation) ---
        _errorLabel = new Label
        {
            Text = "",
            ForeColor = Color.Red,
            Font = new Font("Microsoft YaHei", 9F),
            Location = new Point(130, 170),
            Size = new Size(230, 28),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // --- Buttons ---
        _loginButton = new Button
        {
            Text = "登录",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(155, 210),
            Size = new Size(95, 36),
        };
        _loginButton.Click += OnLoginClick;

        _cancelButton = new Button
        {
            Text = "取消",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(270, 210),
            Size = new Size(95, 36),
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            titleLabel,
            roleLabel, _roleCombo,
            passwordLabel, _passwordTextBox,
            _errorLabel,
            _loginButton, _cancelButton,
        });

        AcceptButton = _loginButton;
        CancelButton = _cancelButton;
        ActiveControl = _passwordTextBox;
    }

    private void OnLoginClick(object? sender, EventArgs e)
    {
        var (success, errorMessage) = _auth.TryLogin(_roleCombo.Text, _passwordTextBox.Text);

        if (success)
        {
            LoginResult = new LoginResult(_roleCombo.Text, _roleCombo.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            // Error display path is wired but currently unused
            // (AuthCoordinator is a shell that always succeeds).
            // Will be activated when real validation is added.
            _errorLabel.Text = errorMessage ?? "登录失败";
        }
    }
}
