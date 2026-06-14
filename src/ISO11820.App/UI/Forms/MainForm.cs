using ISO11820.App.App;

namespace ISO11820.App.UI.Forms;

public sealed class MainForm : Form
{
    private readonly Iso11820AppContext _appContext;
    private readonly Label _statusLabel;

    public MainForm(Iso11820AppContext appContext)
    {
        _appContext = appContext;
        Text = "ISO 11820 仿真系统";
        Width = 1280;
        Height = 800;

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "项目骨架已初始化。"
        };

        Controls.Add(_statusLabel);
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _appContext.TestController.DataBroadcast += OnDataBroadcast;
        _appContext.DaqWorker.Start();
    }

    private void OnDataBroadcast(object? sender, Shared.Events.DataBroadcastEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDataBroadcast(sender, e));
            return;
        }

        _statusLabel.Text = $"当前状态：{e.Snapshot.State} / 炉温1：{e.Snapshot.Temperatures.Furnace1:F1}°C";
    }
}
