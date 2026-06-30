using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using System.Drawing;

namespace ISO11820.App.Features.Export;

/// <summary>
/// 试验信息摘要（用于 Excel/PDF 导出）
/// </summary>
public sealed record TestInfoSummary
{
    public required string TestId { get; init; }
    public required string ProductId { get; init; }
    public string Operator { get; init; } = "";
    public DateTime TestTime { get; init; } = DateTime.Now;
    public int DurationSeconds { get; init; }
}

/// <summary>
/// Excel 导出服务，使用 EPPlus 生成包含 3 个 Sheet 的 Excel 文件
/// </summary>
public sealed class ExcelExportService
{
    /// <summary>
    /// 生成 Excel 文件
    /// </summary>
    public ExportResult GenerateExcel(
        string outputPath,
        TestInfoSummary testInfo,
        IReadOnlyList<CsvRow> data,
        Image? chartImage)
    {
        try
        {
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Directory?.Create();

            using var package = new ExcelPackage();

            // Sheet 1: 试验信息
            CreateInfoSheet(package, testInfo);

            // Sheet 2: 温度数据
            CreateDataSheet(package, data);

            // Sheet 3: 图表
            if (chartImage != null)
            {
                CreateChartSheet(package, chartImage);
            }

            package.SaveAs(fileInfo);
            return ExportResult.Succeeded(outputPath, ExportFormat.Excel);
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"生成 Excel 失败: {ex.Message}");
        }
    }

    private static void CreateInfoSheet(ExcelPackage package, TestInfoSummary testInfo)
    {
        var ws = package.Workbook.Worksheets.Add("试验信息");

        ws.Cells[1, 1].Value = "ISO 11820 试验报告";
        ws.Cells[1, 1].Style.Font.Size = 16;
        ws.Cells[1, 1].Style.Font.Bold = true;
        ws.Cells["A1:C1"].Merge = true;

        ws.Cells[3, 1].Value = "试验编号";
        ws.Cells[3, 2].Value = testInfo.TestId;

        ws.Cells[4, 1].Value = "样品编号";
        ws.Cells[4, 2].Value = testInfo.ProductId;

        ws.Cells[5, 1].Value = "操作员";
        ws.Cells[5, 2].Value = testInfo.Operator;

        ws.Cells[6, 1].Value = "试验时间";
        ws.Cells[6, 2].Value = testInfo.TestTime.ToString("yyyy-MM-dd HH:mm:ss");

        ws.Cells[7, 1].Value = "试验时长";
        ws.Cells[7, 2].Value = $"{testInfo.DurationSeconds} 秒";

        ws.Column(1).Width = 15;
        ws.Column(2).Width = 30;
    }

    private static void CreateDataSheet(ExcelPackage package, IReadOnlyList<CsvRow> data)
    {
        var ws = package.Workbook.Worksheets.Add("温度数据");

        // 表头
        ws.Cells[1, 1].Value = "时间 (秒)";
        ws.Cells[1, 2].Value = "炉温1 (°C)";
        ws.Cells[1, 3].Value = "炉温2 (°C)";
        ws.Cells[1, 4].Value = "表面温 (°C)";
        ws.Cells[1, 5].Value = "中心温 (°C)";

        ws.Cells[1, 1, 1, 5].Style.Font.Bold = true;

        // 数据行
        for (int i = 0; i < data.Count; i++)
        {
            var row = data[i];
            ws.Cells[i + 2, 1].Value = row.ElapsedSeconds;
            ws.Cells[i + 2, 2].Value = row.Furnace1;
            ws.Cells[i + 2, 3].Value = row.Furnace2;
            ws.Cells[i + 2, 4].Value = row.Surface;
            ws.Cells[i + 2, 5].Value = row.Center;
        }

        // 设置列宽
        ws.Column(1).Width = 12;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 14;
    }

    private static void CreateChartSheet(ExcelPackage package, Image chartImage)
    {
        var ws = package.Workbook.Worksheets.Add("温度曲线");

        // 将图片保存为临时文件然后嵌入
        var tempPath = Path.Combine(Path.GetTempPath(), $"chart_{Guid.NewGuid()}.png");
        try
        {
            chartImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            var picture = ws.Drawings.AddPicture("TemperatureChart", new FileInfo(tempPath));
            picture.SetPosition(1, 0, 1, 0);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }
}
