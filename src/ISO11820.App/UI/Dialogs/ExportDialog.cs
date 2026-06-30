using ISO11820.App.Features.Export;

namespace ISO11820.App.UI.Dialogs;

public sealed class ExportDialog : Form
{
    private readonly ExportCoordinator _export;
    private readonly string _productId;
    private readonly string _testId;
    private readonly Func<System.Drawing.Image?> _chartImageProvider;

    private TextBox _txtProductId = null!;
    private TextBox _txtTestId = null!;
    private Label _lblStatus = null!;

    public ExportDialog(ExportCoordinator export, string productId, string testId,
        Func<System.Drawing.Image?> chartImageProvider)
    {
        _export = export ?? throw new ArgumentNullException(nameof(export));
        _productId = productId ?? string.Empty;
        _testId = testId ?? string.Empty;
        _chartImageProvider = chartImageProvider ?? throw new ArgumentNullException(nameof(chartImageProvider));

        Text = "试验记录导出";
        Width = 550;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei", 10F);
        AutoScaleMode = AutoScaleMode.Font;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var padding = 24;
        var inputX = 150;
        var inputWidth = 360;
        var btnWidth = 140;
        var btnGap = 170;
        var rowHeight = 36;
        var y = padding;
        var contentWidth = ClientSize.Width - padding * 2;

        // 标题
        var lblTitle = new Label
        {
            Text = "导出试验记录",
            Font = new Font("Microsoft YaHei", 13F, FontStyle.Bold),
            Location = new Point(padding, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(0, 100, 180),
        };
        Controls.Add(lblTitle);
        y += 44;

        // 产品编号
        var lblProduct = new Label
        {
            Text = "产品编号：",
            Location = new Point(padding, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtProductId = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputWidth, rowHeight),
            Text = _productId,
            ReadOnly = true,
        };
        Controls.AddRange(new Control[] { lblProduct, _txtProductId });
        y += rowHeight + 8;

        // 试验标识
        var lblTestId = new Label
        {
            Text = "试验标识：",
            Location = new Point(padding, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _txtTestId = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputWidth, rowHeight),
            Text = _testId,
            ReadOnly = true,
        };
        Controls.AddRange(new Control[] { lblTestId, _txtTestId });
        y += rowHeight + 16;

        // 分隔线
        var separator = new Label
        {
            Text = "",
            Location = new Point(padding, y),
            Size = new Size(contentWidth, 2),
            BorderStyle = BorderStyle.Fixed3D,
            Height = 2,
        };
        Controls.Add(separator);
        y += 16;

        // 导出格式选择
        var lblFormat = new Label
        {
            Text = "导出格式：",
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(padding, y + 3),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(lblFormat);
        y += rowHeight + 8;

        // CSV 导出按钮
        var btnCsv = new Button
        {
            Text = "导出 CSV",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(inputX, y),
            Size = new Size(btnWidth, 40),
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        btnCsv.FlatAppearance.BorderSize = 0;
        btnCsv.Click += (_, _) => ExportCsv();
        Controls.Add(btnCsv);

        // Excel 导出按钮
        var btnExcel = new Button
        {
            Text = "导出 Excel",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(inputX + btnGap, y),
            Size = new Size(btnWidth, 40),
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        btnExcel.FlatAppearance.BorderSize = 0;
        btnExcel.Click += (_, _) => ExportExcel();
        Controls.Add(btnExcel);
        y += rowHeight + 16;

        // PDF 导出按钮
        var btnPdf = new Button
        {
            Text = "导出 PDF",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(inputX, y),
            Size = new Size(btnWidth, 40),
            BackColor = Color.FromArgb(178, 34, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        btnPdf.FlatAppearance.BorderSize = 0;
        btnPdf.Click += (_, _) => ExportPdf();
        Controls.Add(btnPdf);

        // 打开目录按钮
        var btnOpenDir = new Button
        {
            Text = "打开导出目录",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(inputX + btnGap, y),
            Size = new Size(btnWidth, 40),
        };
        btnOpenDir.Click += (_, _) => OpenExportDirectory();
        Controls.Add(btnOpenDir);
        y += rowHeight + 16;

        // 状态标签
        _lblStatus = new Label
        {
            Location = new Point(padding, y),
            Size = new Size(contentWidth, 42),
            Text = "",
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(_lblStatus);

        // 关闭按钮
        var btnClose = new Button
        {
            Text = "关闭",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(padding + inputWidth + btnWidth - 90, y + 8),
            Size = new Size(90, 36),
        };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        // 动态调整窗体高度
        ClientSize = new Size(ClientSize.Width, y + 70);
    }

    private void ExportCsv()
    {
        var request = new ExportRequest
        {
            ProductId = _productId,
            TestId = _testId,
        };
        var result = _export.ExportToCsv(request);
        ShowResult(result, "CSV");
    }

    private void ExportExcel()
    {
        var request = new ExportRequest
        {
            ProductId = _productId,
            TestId = _testId,
            ChartImage = _chartImageProvider(),
        };
        var result = _export.ExportToExcel(request);
        ShowResult(result, "Excel");
    }

    private void ExportPdf()
    {
        var request = new ExportRequest
        {
            ProductId = _productId,
            TestId = _testId,
            ChartImage = _chartImageProvider(),
        };
        var result = _export.ExportToPdf(request);
        ShowResult(result, "PDF");
    }

    private void ShowResult(ExportResult result, string formatName)
    {
        if (result.Success)
        {
            _lblStatus.ForeColor = Color.Green;
            _lblStatus.Text = $"{formatName} 导出成功: {result.FilePath}";
            MessageBox.Show(
                $"{formatName} 导出成功！\n\n文件路径：\n{result.FilePath}",
                "导出成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = $"{formatName} 导出失败: {result.Error}";
            MessageBox.Show(
                $"{formatName} 导出失败！\n\n{result.Error}",
                "导出失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OpenExportDirectory()
    {
        try
        {
            var directory = _export.GetOutputDirectory(_productId, _testId);
            if (Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start("explorer.exe", directory);
            }
            else
            {
                _lblStatus.ForeColor = Color.Red;
                _lblStatus.Text = "导出目录不存在，请先执行导出操作。";
            }
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = $"打开目录失败: {ex.Message}";
        }
    }
}