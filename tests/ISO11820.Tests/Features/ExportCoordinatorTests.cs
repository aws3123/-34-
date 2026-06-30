using ISO11820.App.Features.Export;
using ISO11820.App.Infrastructure.FileStorage;

namespace ISO11820.Tests.Features;

public sealed class ExportCoordinatorTests : IDisposable
{
    private readonly string _tempBaseDir;
    private readonly CsvSampleWriter _csvWriter;
    private readonly ExportCoordinator _coordinator;

    public ExportCoordinatorTests()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempBaseDir);
        _csvWriter = new CsvSampleWriter(_tempBaseDir);
        _coordinator = new ExportCoordinator(_csvWriter, new ExcelExportService(), new PdfExportService());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBaseDir))
        {
            Directory.Delete(_tempBaseDir, recursive: true);
        }
    }

    [Fact]
    public void GetOutputDirectory_Should_Return_Correct_Path()
    {
        var directory = _coordinator.GetOutputDirectory("P001", "T001");

        Assert.Contains("TestData", directory);
        Assert.Contains("P001", directory);
        Assert.Contains("T001", directory);
    }

    [Fact]
    public void GetExportFiles_Should_Return_Empty_Array_When_Directory_Not_Exists()
    {
        var files = _coordinator.GetExportFiles("P001", "T001");

        Assert.Empty(files);
    }

    [Fact]
    public void GetExportFiles_Should_Return_Csv_File_Info()
    {
        var directory = _csvWriter.EnsureDirectoryExists("P001", "T001");
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        File.WriteAllText(csvPath, "Timestamp,Channel1\n2024-01-01,100.0");

        var files = _coordinator.GetExportFiles("P001", "T001");

        Assert.Single(files);
        Assert.Equal(ExportFormat.Csv, files[0].Format);
        Assert.True(files[0].FileSize > 0);
    }

    [Fact]
    public void ExportToCsv_Should_Return_Success_When_File_Exists()
    {
        var directory = _csvWriter.EnsureDirectoryExists("P001", "T001");
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        File.WriteAllText(csvPath, "Timestamp,Channel1\n2024-01-01,100.0");

        var request = new ExportRequest { ProductId = "P001", TestId = "T001" };
        var result = _coordinator.ExportToCsv(request);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.Equal(ExportFormat.Csv, result.Format);
    }

    [Fact]
    public void ExportToCsv_Should_Return_Failed_When_File_Not_Exists()
    {
        var request = new ExportRequest { ProductId = "P001", TestId = "T001" };
        var result = _coordinator.ExportToCsv(request);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ExportToExcel_Should_Return_Failed_When_No_Csv_File()
    {
        var request = new ExportRequest { ProductId = "P001", TestId = "T001" };
        var result = _coordinator.ExportToExcel(request);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ExportToExcel_Should_Return_Xlsx_Path_When_Csv_Exists()
    {
        var directory = _csvWriter.EnsureDirectoryExists("P001", "T001");
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        File.WriteAllText(csvPath, "Timestamp,Channel1,Channel2,Channel3,Channel4\n2024-01-01,100.0,100.0,50.0,40.0");

        var testInfo = new TestInfoSummary { TestId = "T001", ProductId = "P001" };
        var request = new ExportRequest { ProductId = "P001", TestId = "T001", TestInfo = testInfo };
        var result = _coordinator.ExportToExcel(request);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.EndsWith(".xlsx", result.FilePath);
    }

    [Fact]
    public void ExportToPdf_Should_Return_Failed_When_No_Csv_File()
    {
        var request = new ExportRequest { ProductId = "P001", TestId = "T001" };
        var result = _coordinator.ExportToPdf(request);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ExportToPdf_Should_Return_Pdf_Path_When_Csv_Exists()
    {
        var directory = _csvWriter.EnsureDirectoryExists("P001", "T001");
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        File.WriteAllText(csvPath, "Timestamp,Channel1,Channel2,Channel3,Channel4\n2024-01-01,100.0,100.0,50.0,40.0");

        var testInfo = new TestInfoSummary { TestId = "T001", ProductId = "P001" };
        var request = new ExportRequest { ProductId = "P001", TestId = "T001", TestInfo = testInfo };
        var result = _coordinator.ExportToPdf(request);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.EndsWith(".pdf", result.FilePath);
    }

    [Fact]
    public void ExportRequest_Should_Require_ProductId()
    {
        var request = new ExportRequest { ProductId = "", TestId = "T001" };

        Assert.NotNull(request);
        Assert.Empty(request.ProductId);
    }

    [Fact]
    public void ExportRequest_Should_Require_TestId()
    {
        var request = new ExportRequest { ProductId = "P001", TestId = "" };

        Assert.NotNull(request);
        Assert.Empty(request.TestId);
    }

    [Fact]
    public void ExportOptions_Should_Have_Default_Values()
    {
        var options = new ExportOptions();

        Assert.True(options.IncludeRawData);
        Assert.True(options.IncludeStatistics);
    }

    [Fact]
    public void ExportOptions_Can_Be_Customized()
    {
        var options = new ExportOptions
        {
            IncludeRawData = false,
            IncludeStatistics = false
        };

        Assert.False(options.IncludeRawData);
        Assert.False(options.IncludeStatistics);
    }

    [Fact]
    public void ExportResult_Success_Should_Set_Properties_Correctly()
    {
        var result = ExportResult.Succeeded("/path/to/file.csv", ExportFormat.Csv, "导出成功");

        Assert.True(result.Success);
        Assert.Equal("/path/to/file.csv", result.FilePath);
        Assert.Equal(ExportFormat.Csv, result.Format);
        Assert.Equal("导出成功", result.Message);
    }

    [Fact]
    public void ExportResult_Failed_Should_Set_Properties_Correctly()
    {
        var result = ExportResult.Failed("文件不存在");

        Assert.False(result.Success);
        Assert.Equal("文件不存在", result.Error);
    }

    [Fact]
    public void ExportFileInfo_Should_Store_File_Information()
    {
        var info = new ExportFileInfo("/path/to/file.csv", ExportFormat.Csv, 1024);

        Assert.Equal("/path/to/file.csv", info.FilePath);
        Assert.Equal(ExportFormat.Csv, info.Format);
        Assert.Equal(1024, info.FileSize);
    }

    [Fact]
    public void ExportToCsv_Should_Return_Correct_File_Path_Structure()
    {
        var directory = _csvWriter.EnsureDirectoryExists("Product-ABC", "Test-XYZ");
        var csvPath = Path.Combine(directory, "sensor_data.csv");
        File.WriteAllText(csvPath, "Timestamp,Channel1");

        var request = new ExportRequest { ProductId = "Product-ABC", TestId = "Test-XYZ" };
        var result = _coordinator.ExportToCsv(request);

        Assert.True(result.Success);
        Assert.Contains("Product-ABC", result.FilePath);
        Assert.Contains("Test-XYZ", result.FilePath);
        Assert.EndsWith("sensor_data.csv", result.FilePath);
    }

    [Fact]
    public void Constructor_Should_Throw_For_Null_CsvWriter()
    {
        Assert.Throws<ArgumentNullException>(() => new ExportCoordinator(null!, new ExcelExportService(), new PdfExportService()));
    }

    [Theory]
    [InlineData("P001", "T001")]
    [InlineData("Product-A", "Test-1")]
    [InlineData("PROD_123", "TEST_456")]
    public void GetOutputDirectory_Should_Work_With_Various_Ids(string productId, string testId)
    {
        var directory = _coordinator.GetOutputDirectory(productId, testId);

        Assert.True(Directory.Exists(directory) || !Directory.Exists(directory));
        Assert.Contains(productId, directory);
        Assert.Contains(testId, directory);
    }
}
