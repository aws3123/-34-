using System.Globalization;
using System.Text;

namespace ISO11820.App.Infrastructure.FileStorage;

/// <summary>
/// CSV 采样数据写入器
/// 负责生成符合规范的目录结构和 CSV 文件输出
/// 路径格式：{baseDirectory}/TestData/{productId}/{testId}/sensor_data.csv
/// </summary>
public sealed class CsvSampleWriter
{
    private readonly string _baseDirectory;
    private const string TestDataFolder = "TestData";
    private const string CsvFileName = "sensor_data.csv";

    public CsvSampleWriter(string baseDirectory)
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
    }

    /// <summary>
    /// 构建测试数据目录路径
    /// </summary>
    /// <param name="productId">产品编号</param>
    /// <param name="testId">试验编号</param>
    /// <returns>完整目录路径</returns>
    public string BuildTestDirectory(string productId, string testId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);

        return Path.Combine(_baseDirectory, TestDataFolder, productId, testId);
    }

    /// <summary>
    /// 构建 CSV 文件完整路径
    /// </summary>
    /// <param name="productId">产品编号</param>
    /// <param name="testId">试验编号</param>
    /// <returns>CSV 文件完整路径</returns>
    public string BuildCsvFilePath(string productId, string testId)
    {
        var directory = BuildTestDirectory(productId, testId);
        return Path.Combine(directory, CsvFileName);
    }

    /// <summary>
    /// 确保目录存在，如不存在则创建
    /// </summary>
    /// <param name="productId">产品编号</param>
    /// <param name="testId">试验编号</param>
    /// <returns>创建的目录路径</returns>
    public string EnsureDirectoryExists(string productId, string testId)
    {
        var directory = BuildTestDirectory(productId, testId);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    /// <summary>
    /// 写入传感器数据到 CSV 文件
    /// </summary>
    /// <param name="productId">产品编号</param>
    /// <param name="testId">试验编号</param>
    /// <param name="records">传感器记录数据</param>
    public void WriteSensorData(string productId, string testId, IEnumerable<SensorDataRecord> records)
    {
        EnsureDirectoryExists(productId, testId);
        var filePath = BuildCsvFilePath(productId, testId);

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        // 写入 CSV 头部
        writer.WriteLine("Timestamp,Channel1,Channel2,Channel3,Channel4,Channel5,Channel6,Channel7,Channel8,Channel9,Channel10,Channel11,Channel12");

        foreach (var record in records)
        {
            var line = FormatSensorRecord(record);
            writer.WriteLine(line);
        }
    }

    /// <summary>
    /// 格式化单条传感器记录为 CSV 行
    /// </summary>
    private string FormatSensorRecord(SensorDataRecord record)
    {
        var sb = new StringBuilder();
        sb.Append(record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

        foreach (var value in record.ChannelValues)
        {
            sb.Append(CultureInfo.InvariantCulture, $",{value:F2}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 检查 CSV 文件是否已存在
    /// </summary>
    public bool CsvFileExists(string productId, string testId)
    {
        var filePath = BuildCsvFilePath(productId, testId);
        return File.Exists(filePath);
    }
}

/// <summary>
/// 传感器数据记录模型
/// </summary>
public sealed record SensorDataRecord
{
    public DateTime Timestamp { get; init; }
    public double[] ChannelValues { get; init; } = Array.Empty<double>();
}
