using System.Drawing;

namespace ISO11820.App.Features.Export;

/// <summary>
/// PDF 导出服务（功能 4 实现）
/// </summary>
public sealed class PdfExportService
{
    public ExportResult GeneratePdf(
        string outputPath,
        TestInfoSummary testInfo,
        TestMetrics metrics,
        Image? chartImage)
    {
        try
        {
            // 将在功能 4 中实现完整逻辑
            // 暂时返回占位结果
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 使用 MigraDoc 生成 PDF
            GeneratePdfInternal(outputPath, testInfo, metrics, chartImage);

            return ExportResult.Succeeded(outputPath, ExportFormat.Pdf);
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"生成 PDF 失败: {ex.Message}");
        }
    }

    private void GeneratePdfInternal(
        string outputPath,
        TestInfoSummary testInfo,
        TestMetrics metrics,
        Image? chartImage)
    {
        var document = new MigraDoc.DocumentObjectModel.Document();
        var section = document.AddSection();

        // 标题
        var title = section.AddParagraph("ISO 11820 试验报告");
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Alignment = MigraDoc.DocumentObjectModel.ParagraphAlignment.Center;
        title.Format.SpaceAfter = "1cm";

        // 试验信息表
        var infoTable = section.AddTable();
        infoTable.Borders.Width = 0.5;
        infoTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(4));
        infoTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(10));

        AddTableRow(infoTable, "试验编号", testInfo.TestId);
        AddTableRow(infoTable, "样品编号", testInfo.ProductId);
        AddTableRow(infoTable, "操作员", testInfo.Operator);
        AddTableRow(infoTable, "试验时间", testInfo.TestTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddTableRow(infoTable, "试验时长", $"{testInfo.DurationSeconds} 秒");

        section.AddParagraph("");

        // 试验指标
        var metricsTitle = section.AddParagraph("试验指标");
        metricsTitle.Format.Font.Size = 14;
        metricsTitle.Format.Font.Bold = true;
        metricsTitle.Format.SpaceBefore = "0.5cm";

        var metricsTable = section.AddTable();
        metricsTable.Borders.Width = 0.5;
        metricsTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(6));
        metricsTable.AddColumn(MigraDoc.DocumentObjectModel.Unit.FromCentimeter(8));

        AddTableRow(metricsTable, "炉温温升 (°C)", $"{metrics.DeltaTf:F1}");
        AddTableRow(metricsTable, "失重率 (%)", $"{metrics.LostWeightPercent:F2}");
        AddTableRow(metricsTable, "火焰持续时间 (秒)", $"{metrics.FlameDurationSeconds:F1}");

        section.AddParagraph("");

        // 判定结论
        var judgmentTitle = section.AddParagraph("判定结论");
        judgmentTitle.Format.Font.Size = 14;
        judgmentTitle.Format.Font.Bold = true;

        var judgment = section.AddParagraph(metrics.JudgmentText);
        judgment.Format.Font.Size = 16;
        judgment.Format.Font.Bold = true;
        judgment.Format.Font.Color = metrics.IsQualified
            ? MigraDoc.DocumentObjectModel.Colors.Green
            : MigraDoc.DocumentObjectModel.Colors.Red;

        // 嵌入图表图片
        if (chartImage != null)
        {
            section.AddParagraph("");
            var chartTitle = section.AddParagraph("温度曲线");
            chartTitle.Format.Font.Size = 14;
            chartTitle.Format.Font.Bold = true;

            var tempPath = Path.Combine(Path.GetTempPath(), $"pdf_chart_{Guid.NewGuid()}.png");
            try
            {
                chartImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                var image = section.AddImage(tempPath);
                image.Width = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(16);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        // 保存 PDF
        var pdfRenderer = new PdfSharp.Pdf.PdfDocument();
        var renderer = new MigraDoc.Rendering.PdfDocumentRenderer(true)
        {
            Document = document
        };
        renderer.PdfDocument = pdfRenderer;
        renderer.RenderDocument();
        pdfRenderer.Save(outputPath);
    }

    private static void AddTableRow(MigraDoc.DocumentObjectModel.Tables.Table table, string label, string value)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(label);
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[1].AddParagraph(value);
    }
}
