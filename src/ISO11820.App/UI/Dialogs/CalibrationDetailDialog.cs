using ISO11820.App.UI.Panels;

namespace ISO11820.App.UI.Dialogs;

public sealed class CalibrationDetailDialog : Form
{
    public CalibrationDetailDialog(
        string calibrationDate,
        string? technician,
        string? notes,
        List<CalibrationPoint> points)
    {
        Text = "校准记录详情";
        Width = 580;
        Height = 440;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Font;

        // Metadata panel
        var metadataPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 10, 12, 4)
        };
        metadataPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        metadataPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        metadataPanel.Controls.Add(new Label { Text = "校准日期：", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        metadataPanel.Controls.Add(new Label { Text = calibrationDate, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
        metadataPanel.Controls.Add(new Label { Text = "操作员：", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
        metadataPanel.Controls.Add(new Label { Text = technician ?? "—", TextAlign = ContentAlignment.MiddleLeft }, 1, 1);
        metadataPanel.Controls.Add(new Label { Text = "备注：", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        metadataPanel.Controls.Add(new Label { Text = notes ?? "—", TextAlign = ContentAlignment.MiddleLeft }, 1, 2);

        // Points grid
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D,
            Font = new Font("Microsoft YaHei UI", 9.5f)
        };

        grid.Columns.Add("Index", "#");
        grid.Columns["Index"].Width = 40;
        grid.Columns["Index"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        grid.Columns.Add("Ref", "标准温度(°C)");
        grid.Columns.Add("Measured", "实测温度(°C)");
        grid.Columns.Add("Deviation", "偏差(°C)");
        grid.Columns.Add("Time", "时间");

        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            grid.Rows.Add(
                i + 1,
                $"{p.Ref:F1}",
                $"{p.Measured:F1}",
                $"{p.Deviation:+#0.0;-#0.0;0.0}",
                p.Time);
        }

        // Close button
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 12, 8)
        };

        var okButton = new Button
        {
            Text = "关闭",
            Width = 80,
            Height = 30
        };
        okButton.Click += (_, _) => Close();
        buttonPanel.Controls.Add(okButton);

        Controls.Add(grid);
        Controls.Add(buttonPanel);
        Controls.Add(metadataPanel);
    }
}