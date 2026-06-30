using ISO11820.App.Infrastructure.Persistence.Models;

namespace ISO11820.App.UI.Dialogs;

public sealed class TestDetailDialog : Form
{
    public TestDetailDialog(TestMaster test)
    {
        Text = "试验详情";
        Size = new Size(520, 620);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 17,
            Padding = new Padding(16),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(mainPanel, 0, "样品编号", test.ProductId);
        AddRow(mainPanel, 1, "试验标识", test.TestId);
        AddRow(mainPanel, 2, "试验日期", test.TestDate);
        AddRow(mainPanel, 3, "操作员", test.Operator ?? "-");
        AddRow(mainPanel, 4, "样品名称", test.SampleName ?? "-");
        AddRow(mainPanel, 5, "规格", test.Specification ?? "-");
        AddRow(mainPanel, 6, "高度 / 直径", $"{test.HeightMm:F1} mm / {test.DiameterMm:F1} mm");
        AddRow(mainPanel, 7, "试验前/后重量", $"{test.PreWeight:F1} g / {test.PostWeight:F1} g");
        AddRow(mainPanel, 8, "质量损失率", test.LostWeightPer.HasValue ? $"{test.LostWeightPer:F2}%" : "-");
        AddRow(mainPanel, 9, "ΔTf", test.DeltaTf.HasValue ? $"{test.DeltaTf:F2} °C" : "-");
        AddRow(mainPanel, 10, "试验总时间", test.TotalTestTime.HasValue ? $"{test.TotalTestTime} s" : "-");
        AddRow(mainPanel, 11, "火焰出现时间", test.FlameTime.HasValue ? $"{test.FlameTime} s" : "-");
        AddRow(mainPanel, 12, "火焰持续时间", test.FlameDuration.HasValue ? $"{test.FlameDuration} s" : "-");
        AddRow(mainPanel, 13, "是否有火焰", test.HasFlame == 1 ? "是" : "否");
        AddRow(mainPanel, 14, "环境温度", test.EnvTemp.HasValue ? $"{test.EnvTemp:F1} °C" : "-");
        AddRow(mainPanel, 15, "环境湿度", test.EnvHumidity.HasValue ? $"{test.EnvHumidity:F1}%" : "-");
        AddRow(mainPanel, 16, "备注", test.Notes ?? "-");

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
        };
        var closeButton = new Button
        {
            Text = "关闭",
            Size = new Size(80, 32),
            Location = new Point(420, 8),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
        };
        closeButton.Click += (_, _) => Close();
        buttonPanel.Controls.Add(closeButton);

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private static void AddRow(TableLayoutPanel panel, int row, string label, string value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var lbl = new Label
        {
            Text = label + "：",
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(80, 80, 80),
        };
        var val = new Label
        {
            Text = value,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei", 9F),
            AutoSize = false,
            Dock = DockStyle.Fill,
        };
        panel.Controls.Add(lbl, 0, row);
        panel.Controls.Add(val, 1, row);
    }
}