using System.Globalization;
using ISO11820.App.Infrastructure.FileStorage;

namespace ISO11820.Tests.Persistence;

/// <summary>
/// CSV 采样写入器测试
/// 验证路径生成、目录创建和文件写入功能
/// </summary>
public sealed class CsvSampleWriterTests : IDisposable
{
    private readonly string _tempBaseDir;
    private readonly CsvSampleWriter _writer;

    public CsvSampleWriterTests()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempBaseDir);
        _writer = new CsvSampleWriter(_tempBaseDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBaseDir))
        {
            Directory.Delete(_tempBaseDir, recursive: true);
        }
    }

    [Fact]
    public void BuildTestDirectory_Should_Combine_BaseDirectory_ProductId_And_TestId()
    {
        var path = _writer.BuildTestDirectory("P001", "T001");

        Assert.Contains("TestData", path);
        Assert.Contains("P001", path);
        Assert.Contains("T001", path);
    }

    [Theory]
    [InlineData("P001", "T001")]
    [InlineData("Product-A123", "Test-2024-001")]
    [InlineData("PROD_001", "TEST_001")]
    public void BuildTestDirectory_Should_Generate_Correct_Structure(string productId, string testId)
    {
        var path = _writer.BuildTestDirectory(productId, testId);
        var expectedEnding = Path.Combine("TestData", productId, testId);

        Assert.EndsWith(expectedEnding, path);
    }

    [Fact]
    public void BuildCsvFilePath_Should_End_With_Sensor_Data_Csv()
    {
        var path = _writer.BuildCsvFilePath("P001", "T001");

        Assert.EndsWith("sensor_data.csv", path);
    }

    [Fact]
    public void BuildCsvFilePath_Should_Follow_Full_Path_Specification()
    {
        // 规范：{BaseDirectory}/TestData/{productid}/{testid}/sensor_data.csv
        var path = _writer.BuildCsvFilePath("P001", "T001");

        Assert.Contains("TestData", path);
        Assert.Contains("P001", path);
        Assert.Contains("T001", path);
        Assert.EndsWith("sensor_data.csv", path);
    }

    [Fact]
    public void EnsureDirectoryExists_Should_Create_Directory_If_Not_Exists()
    {
        var directory = _writer.EnsureDirectoryExists("P001", "T001");

        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public void EnsureDirectoryExists_Should_Return_Existing_Directory()
    {
        _writer.EnsureDirectoryExists("P001", "T001");

        var directory = _writer.EnsureDirectoryExists("P001", "T001");

        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public void CsvFileExists_Should_Return_False_For_New_Test()
    {
        var exists = _writer.CsvFileExists("P001", "T001");

        Assert.False(exists);
    }

    [Fact]
    public void WriteSensorData_Should_Create_Csv_File()
    {
        var records = new[]
        {
            new SensorDataRecord
            {
                Timestamp = DateTime.Now,
                ChannelValues = new[] { 720.0, 725.0, 730.0, 735.0, 740.0, 745.0, 750.0, 755.0, 760.0, 765.0, 770.0, 775.0 }
            }
        };

        _writer.WriteSensorData("P001", "T001", records);

        var filePath = _writer.BuildCsvFilePath("P001", "T001");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void WriteSensorData_Should_Write_Correct_Csv_Header()
    {
        var records = Array.Empty<SensorDataRecord>();

        _writer.WriteSensorData("P001", "T001", records);

        var filePath = _writer.BuildCsvFilePath("P001", "T001");
        var content = File.ReadAllText(filePath);

        Assert.StartsWith("Timestamp,Channel1,Channel2,Channel3,Channel4,Channel5,Channel6,Channel7,Channel8,Channel9,Channel10,Channel11,Channel12", content);
    }

    [Fact]
    public void WriteSensorData_Should_Write_Data_Rows()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, 123);
        var records = new[]
        {
            new SensorDataRecord
            {
                Timestamp = timestamp,
                ChannelValues = new[] { 720.5, 725.25, 730.0, 735.75, 740.5, 745.25, 750.0, 755.75, 760.5, 765.25, 770.0, 775.75 }
            }
        };

        _writer.WriteSensorData("P001", "T001", records);

        var filePath = _writer.BuildCsvFilePath("P001", "T001");
        var lines = File.ReadAllLines(filePath);

        Assert.Equal(2, lines.Length); // header + 1 data row
        Assert.Contains("2024-01-15", lines[1]);
        Assert.Contains("720.50", lines[1]);
    }

    [Fact]
    public void WriteSensorData_Should_Create_Nested_Directory_Structure()
    {
        var records = Array.Empty<SensorDataRecord>();

        _writer.WriteSensorData("Product-ABC", "Test-12345", records);

        var directory = _writer.BuildTestDirectory("Product-ABC", "Test-12345");
        Assert.True(Directory.Exists(directory));
    }

    [Theory]
    [InlineData("", "T001")]
    [InlineData("P001", "")]
    public void BuildTestDirectory_Should_Throw_For_Empty_Inputs(string productId, string testId)
    {
        Assert.Throws<ArgumentException>(() => _writer.BuildTestDirectory(productId, testId));
    }

    [Fact]
    public void WriteSensorData_Should_Handle_Multiple_Records()
    {
        var records = new[]
        {
            new SensorDataRecord { Timestamp = DateTime.Now, ChannelValues = Enumerable.Repeat(720.0, 12).ToArray() },
            new SensorDataRecord { Timestamp = DateTime.Now.AddSeconds(1), ChannelValues = Enumerable.Repeat(721.0, 12).ToArray() },
            new SensorDataRecord { Timestamp = DateTime.Now.AddSeconds(2), ChannelValues = Enumerable.Repeat(722.0, 12).ToArray() }
        };

        _writer.WriteSensorData("P001", "T001", records);

        var filePath = _writer.BuildCsvFilePath("P001", "T001");
        var lines = File.ReadAllLines(filePath);

        Assert.Equal(4, lines.Length); // header + 3 data rows
    }
}
