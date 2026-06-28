using ISO11820.App.Infrastructure.FileStorage;

namespace ISO11820.App.Features.Export;

/// <summary>
/// 导出协调器
/// 负责统一组织试验数据的导出操作，包括 CSV、Excel、PDF 等格式
/// UI 层只负责收集试验标识和触发导出，具体导出逻辑在此实现
/// </summary>
public sealed class ExportCoordinator
{
    private readonly CsvSampleWriter _csvWriter;

    public ExportCoordinator(CsvSampleWriter csvWriter)
    {
        _csvWriter = csvWriter ?? throw new ArgumentNullException(nameof(csvWriter));
    }

    /// <summary>
    /// 导出试验数据为 CSV 格式
    /// </summary>
    /// <param name="request">导出请求参数</param>
    /// <returns>导出结果</returns>
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

    /// <summary>
    /// 导出试验数据为 Excel 格式（预留，后续接入 EPPlus）
    /// </summary>
    /// <param name="request">导出请求参数</param>
    /// <returns>导出结果</returns>
    public ExportResult ExportToExcel(ExportRequest request)
    {
        // 预留：后续接入 EPPlus 或其他 Excel 库
        // 目前返回成功，实际文件路径与 CSV 相同（未来扩展为 .xlsx）
        try
        {
            var directory = _csvWriter.BuildTestDirectory(request.ProductId, request.TestId);
            var excelPath = Path.Combine(directory, "sensor_data.xlsx");

            // TODO: 使用 EPPlus 创建 Excel 文件
            // 目前仅作为接口预留，返回模拟成功
            return ExportResult.Succeeded(excelPath, ExportFormat.Excel, "Excel 导出功能预留，待接入 EPPlus");
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"导出 Excel 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 导出试验报告为 PDF 格式（预留，后续接入 PDFsharp）
    /// </summary>
    /// <param name="request">导出请求参数，包含报告内容</param>
    /// <returns>导出结果</returns>
    public ExportResult ExportToPdf(ExportRequest request)
    {
        // 预留：后续接入 PDFsharp 或其他 PDF 库
        try
        {
            var directory = _csvWriter.BuildTestDirectory(request.ProductId, request.TestId);
            var pdfPath = Path.Combine(directory, "test_report.pdf");

            // TODO: 使用 PDFsharp 创建 PDF 报告
            // 目前仅作为接口预留，返回模拟成功
            return ExportResult.Succeeded(pdfPath, ExportFormat.Pdf, "PDF 导出功能预留，待接入 PDFsharp");
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"导出 PDF 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定试验的所有导出文件路径
    /// </summary>
    /// <param name="productId">产品编号</param>
    /// <param name="testId">试验编号</param>
    /// <returns>导出文件信息集合</returns>
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

        return files.ToArray();
    }

    /// <summary>
    /// 获取试验数据输出目录路径
    /// </summary>
    public string GetOutputDirectory(string productId, string testId)
    {
        return _csvWriter.BuildTestDirectory(productId, testId);
    }
}

/// <summary>
/// 导出请求参数
/// </summary>
public sealed record ExportRequest
{
    /// <summary>产品编号</summary>
    public required string ProductId { get; init; }

    /// <summary>试验编号</summary>
    public required string TestId { get; init; }

    /// <summary>可选：导出附加选项</summary>
    public ExportOptions? Options { get; init; }
}

/// <summary>
/// 导出选项
/// </summary>
public sealed record ExportOptions
{
    /// <summary>是否包含原始数据</summary>
    public bool IncludeRawData { get; init; } = true;

    /// <summary>是否包含统计信息</summary>
    public bool IncludeStatistics { get; init; } = true;
}

/// <summary>
/// 导出结果
/// </summary>
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

/// <summary>
/// 导出文件信息
/// </summary>
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

/// <summary>
/// 导出格式枚举
/// </summary>
public enum ExportFormat
{
    Csv,
    Excel,
    Pdf
}
