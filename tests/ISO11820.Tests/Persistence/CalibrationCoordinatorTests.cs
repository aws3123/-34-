using ISO11820.App.Features.Calibration;
using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Infrastructure.Persistence.Models;
using Microsoft.Data.Sqlite;

namespace ISO11820.Tests.Persistence;

public sealed class CalibrationCoordinatorTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbHelper _dbHelper;
    private readonly DatabaseInitializer _initializer;
    private readonly CalibrationCoordinator _coordinator;

    public CalibrationCoordinatorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_calibration_{Guid.NewGuid()}.db");
        _dbHelper = new DbHelper(_dbPath);
        _initializer = new DatabaseInitializer(_dbHelper);
        _coordinator = new CalibrationCoordinator(_dbHelper);
        _initializer.EnsureCreated();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void SaveRecord_Should_Persist_CalibrationRecord()
    {
        var record = new CalibrationRecord
        {
            SensorId = 1,
            CalibrationDate = "2026-06-27",
            ResultJson = "{\"offset\": 0.5, \"gain\": 1.02}",
            Technician = "admin",
            Notes = "Annual calibration"
        };

        _coordinator.SaveRecord(record);

        var records = _coordinator.QueryRecords();
        Assert.Single(records);
        Assert.Equal(1, records[0].SensorId);
        Assert.Equal("2026-06-27", records[0].CalibrationDate);
        Assert.Contains("offset", records[0].ResultJson);
        Assert.Equal("admin", records[0].Technician);
        Assert.Equal("Annual calibration", records[0].Notes);
    }

    [Fact]
    public void SaveRecord_Should_Handle_Null_Json_Field()
    {
        var record = new CalibrationRecord
        {
            SensorId = 2,
            CalibrationDate = "2026-06-27",
            ResultJson = null,
            Technician = null,
            Notes = null
        };

        _coordinator.SaveRecord(record);

        var records = _coordinator.QueryRecords();
        Assert.Single(records);
        Assert.Null(records[0].ResultJson);
        Assert.Null(records[0].Technician);
        Assert.Null(records[0].Notes);
    }

    [Fact]
    public void QueryRecords_Should_Filter_By_SensorId()
    {
        _coordinator.SaveRecord(new CalibrationRecord
        {
            SensorId = 1,
            CalibrationDate = "2026-06-01",
            ResultJson = "{}"
        });
        _coordinator.SaveRecord(new CalibrationRecord
        {
            SensorId = 2,
            CalibrationDate = "2026-06-02",
            ResultJson = "{}"
        });

        var records = _coordinator.QueryRecords(sensorId: 1);
        Assert.Single(records);
        Assert.Equal(1, records[0].SensorId);
    }

    [Fact]
    public void QueryRecords_Should_Return_All_When_No_Filter()
    {
        _coordinator.SaveRecord(new CalibrationRecord
        {
            SensorId = 1,
            CalibrationDate = "2026-06-01",
            ResultJson = "{}"
        });
        _coordinator.SaveRecord(new CalibrationRecord
        {
            SensorId = 2,
            CalibrationDate = "2026-06-02",
            ResultJson = "{}"
        });

        var records = _coordinator.QueryRecords();
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void CalibrationRecords_Table_Name_Should_Be_Uppercase_C_And_R()
    {
        _coordinator.SaveRecord(new CalibrationRecord
        {
            SensorId = 1,
            CalibrationDate = "2026-06-27",
            ResultJson = "{}"
        });

        using var connection = _dbHelper.CreateConnection();
        using var command = new SqliteCommand(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='CalibrationRecords'", connection);
        var name = command.ExecuteScalar() as string;

        Assert.Equal("CalibrationRecords", name);
    }
}