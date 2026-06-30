using System.Drawing;
using ISO11820.App.Infrastructure.FileStorage;

namespace ISO11820.App.Features.Export;

public sealed class ExportCoordinator
{
    private readonly CsvSampleWriter _csvWriter;
    private readonly ExcelExportService _excelService;
    private readonly PdfExportService _pdfService;
    private readonly CsvDataReader _csvReader;

    public ExportCoordinator(
        CsvSampleWriter csvWriter,
        ExcelExportService excelService,
        PdfExportService pdfService)
    {
        _csvWriter = csvWriter ?? throw new ArgumentNullException(nameof(csvWriter));
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
        _csvReader = new CsvDataReader();
    }

    public ExportResult ExportToCsv(ExportRequest request)
    {
        try
        {
            var filePath = _csvWriter.BuildCsvFilePath(request.ProductId, request.TestId);

            if (!_csvWriter.CsvFileExists(request.ProductId, request.TestId))
            {
                return ExportResult.Failed($"CSV 文件不存在: {filePath}");
            }

            return ExportResult.Succeeded(filePath, ExportFormat.Csv);
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"导出 CSV 失败: {ex.Message}");
        }
    }

    public void SaveSensorDataToCsv(string productId, string testId, IReadOnlyList<SensorDataRecord> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0) return;

        _csvWriter.WriteSensorData(productId, testId, records);
    }

    public ExportResult ExportToExcel(ExportRequest request)
    {
        try
        {
            var csvPath = _csvWriter.BuildCsvFilePath(request.ProductId, request.TestId);
            if (!_csvWriter.CsvFileExists(request.ProductId, request.TestId))
            {
                return ExportResult.Failed($"CSV 文件不存在: {csvPath}");
            }

            var data = _csvReader.ReadAll(csvPath);
            if (data.Count == 0)
            {
                return ExportResult.Failed("CSV 文件为空");
            }

            var directory = _csvWriter.BuildTestDirectory(request.ProductId, request.TestId);
            var excelPath = Path.Combine(directory, "sensor_data.xlsx");

            var testInfo = request.TestInfo ?? new TestInfoSummary
            {
                TestId = request.TestId,
                ProductId = request.ProductId,
            };

            return _excelService.GenerateExcel(excelPath, testInfo, data, request.ChartImage);
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"导出 Excel 失败: {ex.Message}");
        }
    }

    public ExportResult ExportToPdf(ExportRequest request)
    {
        try
        {
            var csvPath = _csvWriter.BuildCsvFilePath(request.ProductId, request.TestId);
            if (!_csvWriter.CsvFileExists(request.ProductId, request.TestId))
            {
                return ExportResult.Failed($"CSV 文件不存在: {csvPath}");
            }

            var directory = _csvWriter.BuildTestDirectory(request.ProductId, request.TestId);
            var pdfPath = Path.Combine(directory, "test_report.pdf");

            var testInfo = request.TestInfo ?? new TestInfoSummary
            {
                TestId = request.TestId,
                ProductId = request.ProductId,
            };

            var metrics = request.Metrics ?? new TestMetrics(
                DeltaTf: 0,
                LostWeightPercent: 0,
                FlameDurationSeconds: 0,
                IsQualified: true,
                JudgmentText: "合格");

            return _pdfService.GeneratePdf(pdfPath, testInfo, metrics, request.ChartImage);
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"导出 PDF 失败: {ex.Message}");
        }
    }

    public ExportFileInfo[] GetExportFiles(string productId, string testId)
    {
        var directory = _csvWriter.BuildTestDirectory(productId, testId);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<ExportFileInfo>();
        }

        var files = new List<ExportFileInfo>();
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        if (File.Exists(csvPath))
        {
            files.Add(new ExportFileInfo(csvPath, ExportFormat.Csv, new FileInfo(csvPath).Length));
        }

        var excelPath = Path.Combine(directory, "sensor_data.xlsx");
        if (File.Exists(excelPath))
        {
            files.Add(new ExportFileInfo(excelPath, ExportFormat.Excel, new FileInfo(excelPath).Length));
        }

        var pdfPath = Path.Combine(directory, "test_report.pdf");
        if (File.Exists(pdfPath))
        {
            files.Add(new ExportFileInfo(pdfPath, ExportFormat.Pdf, new FileInfo(pdfPath).Length));
        }

        return files.ToArray();
    }

    public string GetOutputDirectory(string productId, string testId)
    {
        return _csvWriter.BuildTestDirectory(productId, testId);
    }
}

public sealed record ExportRequest
{
    public required string ProductId { get; init; }
    public required string TestId { get; init; }
    public ExportOptions? Options { get; init; }
    public TestInfoSummary? TestInfo { get; init; }
    public Image? ChartImage { get; init; }
    public TestMetrics? Metrics { get; init; }
}

public sealed record ExportOptions
{
    public bool IncludeRawData { get; init; } = true;
    public bool IncludeStatistics { get; init; } = true;
}

public sealed record ExportResult
{
    public bool Success { get; init; }
    public string? FilePath { get; init; }
    public ExportFormat Format { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }

    public static ExportResult Succeeded(string filePath, ExportFormat format, string? message = null)
        => new() { Success = true, FilePath = filePath, Format = format, Message = message };

    public static ExportResult Failed(string error)
        => new() { Success = false, Error = error };
}

public sealed record ExportFileInfo
{
    public string FilePath { get; }
    public ExportFormat Format { get; }
    public long FileSize { get; }

    public ExportFileInfo(string filePath, ExportFormat format, long fileSize)
    {
        FilePath = filePath;
        Format = format;
        FileSize = fileSize;
    }
}

public enum ExportFormat
{
    Csv,
    Excel,
    Pdf
}

/// <summary>
/// 试验指标（用于 PDF 判定）
/// </summary>
public sealed record TestMetrics(
    double DeltaTf,
    double LostWeightPercent,
    double FlameDurationSeconds,
    bool IsQualified,
    string JudgmentText)
{
    /// <summary>
    /// 根据指标判断是否合格
    /// </summary>
    public static TestMetrics Compute(double deltaTf, double lostWeightPercent, double flameDurationSeconds)
    {
        bool isQualified = deltaTf <= 50 && lostWeightPercent <= 50 && flameDurationSeconds < 5;
        string judgmentText = isQualified ? "合格" : "不合格";
        return new TestMetrics(deltaTf, lostWeightPercent, flameDurationSeconds, isQualified, judgmentText);
    }
}
