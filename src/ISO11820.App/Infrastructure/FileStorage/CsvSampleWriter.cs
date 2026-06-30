using System.Globalization;
using System.Text;

namespace ISO11820.App.Infrastructure.FileStorage;

public sealed class CsvSampleWriter
{
    private readonly string _baseDirectory;
    private const string TestDataFolder = "TestData";
    private const string CsvFileName = "sensor_data.csv";

    public CsvSampleWriter(string baseDirectory)
    {
        _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
    }

    public string BuildTestDirectory(string productId, string testId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);

        return Path.Combine(_baseDirectory, TestDataFolder, productId, testId);
    }

    public string BuildCsvFilePath(string productId, string testId)
    {
        var directory = BuildTestDirectory(productId, testId);
        return Path.Combine(directory, CsvFileName);
    }

    public string EnsureDirectoryExists(string productId, string testId)
    {
        var directory = BuildTestDirectory(productId, testId);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    public void WriteSensorData(string productId, string testId, IEnumerable<SensorDataRecord> records)
    {
        EnsureDirectoryExists(productId, testId);
        var filePath = BuildCsvFilePath(productId, testId);

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("Timestamp,Channel1,Channel2,Channel3,Channel4,Channel5,Channel6,Channel7,Channel8,Channel9,Channel10,Channel11,Channel12");

        foreach (var record in records)
        {
            var line = FormatSensorRecord(record);
            writer.WriteLine(line);
        }
    }

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

    public bool CsvFileExists(string productId, string testId)
    {
        var filePath = BuildCsvFilePath(productId, testId);
        return File.Exists(filePath);
    }
}

public sealed record SensorDataRecord
{
    public DateTime Timestamp { get; init; }
    public double[] ChannelValues { get; init; } = Array.Empty<double>();
}
