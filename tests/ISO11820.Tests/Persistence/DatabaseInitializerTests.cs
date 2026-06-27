using ISO11820.App.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace ISO11820.Tests.Persistence;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbHelper _dbHelper;
    private readonly DatabaseInitializer _initializer;

    public DatabaseInitializerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_iso11820_{Guid.NewGuid()}.db");
        _dbHelper = new DbHelper(_dbPath);
        _initializer = new DatabaseInitializer(_dbHelper);
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
    public void EnsureCreated_Should_Create_Database_File()
    {
        _initializer.EnsureCreated();

        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void EnsureCreated_Should_Create_All_Six_Tables()
    {
        _initializer.EnsureCreated();

        using var connection = _dbHelper.CreateConnection();
        var tables = GetTableNames(connection);

        Assert.Contains("operators", tables);
        Assert.Contains("apparatus", tables);
        Assert.Contains("productmaster", tables);
        Assert.Contains("testmaster", tables);
        Assert.Contains("sensors", tables);
        Assert.Contains("CalibrationRecords", tables);
    }

    [Fact]
    public void EnsureCreated_Should_Seed_Admin_And_Experimenter()
    {
        _initializer.EnsureCreated();

        using var connection = _dbHelper.CreateConnection();
        using var command = new SqliteCommand(
            "SELECT username, role FROM operators ORDER BY id", connection);
        using var reader = command.ExecuteReader();

        var operators = new List<(string username, string role)>();
        while (reader.Read())
        {
            operators.Add((reader.GetString(0), reader.GetString(1)));
        }

        Assert.Equal(2, operators.Count);
        Assert.Contains(("admin", "admin"), operators);
        Assert.Contains(("experimenter", "experimenter"), operators);
    }

    [Fact]
    public void EnsureCreated_Should_Seed_Default_Apparatus()
    {
        _initializer.EnsureCreated();

        using var connection = _dbHelper.CreateConnection();
        using var command = new SqliteCommand(
            "SELECT name, model, serial_number FROM apparatus WHERE id = 1", connection);
        using var reader = command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal("ISO11820 Test Furnace", reader.GetString(0));
        Assert.Equal("ISO11820-M1", reader.GetString(1));
        Assert.Equal("SN-001", reader.GetString(2));
    }

    [Fact]
    public void EnsureCreated_Should_Seed_Five_Default_Sensors()
    {
        _initializer.EnsureCreated();

        using var connection = _dbHelper.CreateConnection();
        using var command = new SqliteCommand(
            "SELECT name, type, channel FROM sensors ORDER BY id", connection);
        using var reader = command.ExecuteReader();

        var sensors = new List<(string name, string type, long channel)>();
        while (reader.Read())
        {
            sensors.Add((reader.GetString(0), reader.GetString(1), reader.GetInt64(2)));
        }

        Assert.Equal(5, sensors.Count);
        Assert.Contains(("Furnace1", "thermocouple", 1), sensors);
        Assert.Contains(("Furnace2", "thermocouple", 2), sensors);
        Assert.Contains(("Surface", "thermocouple", 3), sensors);
        Assert.Contains(("Center", "thermocouple", 4), sensors);
        Assert.Contains(("Calibration", "reference", 5), sensors);
    }

    [Fact]
    public void EnsureCreated_Should_Be_Idempotent()
    {
        _initializer.EnsureCreated();
        _initializer.EnsureCreated();
        _initializer.EnsureCreated();

        using var connection = _dbHelper.CreateConnection();
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM operators", connection);
        var count = (long)command.ExecuteScalar()!;

        Assert.Equal(2, count);
    }

    [Fact]
    public void ValidateLogin_Should_Return_True_For_Valid_Credentials()
    {
        _initializer.EnsureCreated();

        var result = _initializer.ValidateLogin("admin", "123456");

        Assert.True(result);
    }

    [Fact]
    public void ValidateLogin_Should_Return_False_For_Wrong_Password()
    {
        _initializer.EnsureCreated();

        var result = _initializer.ValidateLogin("admin", "wrong");

        Assert.False(result);
    }

    [Fact]
    public void ValidateLogin_Should_Return_False_For_Nonexistent_User()
    {
        _initializer.EnsureCreated();

        var result = _initializer.ValidateLogin("nobody", "123456");

        Assert.False(result);
    }

    [Fact]
    public void ValidateLogin_Should_Query_By_Username_And_Password()
    {
        _initializer.EnsureCreated();

        Assert.True(_initializer.ValidateLogin("experimenter", "123456"));
        Assert.False(_initializer.ValidateLogin("experimenter", "wrong"));
    }

    private static List<string> GetTableNames(SqliteConnection connection)
    {
        using var command = new SqliteCommand(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", connection);
        using var reader = command.ExecuteReader();

        var tables = new List<string>();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}