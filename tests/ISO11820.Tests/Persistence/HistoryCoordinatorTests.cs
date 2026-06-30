using ISO11820.App.Features.History;
using ISO11820.App.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace ISO11820.Tests.Persistence;

public sealed class HistoryCoordinatorTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbHelper _dbHelper;
    private readonly DatabaseInitializer _initializer;
    private readonly HistoryCoordinator _coordinator;

    public HistoryCoordinatorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_history_{Guid.NewGuid()}.db");
        _dbHelper = new DbHelper(_dbPath);
        _initializer = new DatabaseInitializer(_dbHelper);
        _coordinator = new HistoryCoordinator(_dbHelper);
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
    public void QueryOperators_Should_Return_All_Operators()
    {
        var operators = _coordinator.QueryOperators();

        Assert.Equal(2, operators.Count);
        Assert.Contains(operators, o => o.Username == "admin" && o.Role == "admin");
        Assert.Contains(operators, o => o.Username == "experimenter" && o.Role == "experimenter");
    }

    [Fact]
    public void QueryProducts_Should_Return_Empty_When_No_Data()
    {
        var products = _coordinator.QueryProducts();

        Assert.NotNull(products);
        Assert.Empty(products);
    }

    [Fact]
    public void QueryProducts_Should_Filter_By_ProductCode()
    {
        using var connection = _dbHelper.CreateConnection();
        using var cmd = new SqliteCommand(
            "INSERT INTO productmaster (product_code, product_name) VALUES ('P001', 'Test Product')",
            connection);
        cmd.ExecuteNonQuery();

        var products = _coordinator.QueryProducts("P001");
        Assert.Single(products);
        Assert.Equal("P001", products[0].ProductCode);
        Assert.Equal("Test Product", products[0].ProductName);

        var noMatch = _coordinator.QueryProducts("NONEXISTENT");
        Assert.Empty(noMatch);
    }

    [Fact]
    public void QueryTestTypes_Should_Return_Empty_When_No_Data()
    {
        var tests = _coordinator.QueryTestTypes();

        Assert.NotNull(tests);
        Assert.Empty(tests);
    }

    [Fact]
    public void QueryTestTypes_Should_Filter_By_TestType()
    {
        using var connection = _dbHelper.CreateConnection();
        using var cmd = new SqliteCommand(
            "INSERT INTO testmaster (productid, testid, sample_name, specification) VALUES ('P001', 'T001', 'Tensile', 'ISO 6892')",
            connection);
        cmd.ExecuteNonQuery();

        var tests = _coordinator.QueryTestTypes("P001");
        Assert.Single(tests);
        Assert.Equal("P001", tests[0].ProductId);
        Assert.Equal("ISO 6892", tests[0].Specification);
    }
}