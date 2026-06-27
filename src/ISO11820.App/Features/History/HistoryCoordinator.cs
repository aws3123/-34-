using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Infrastructure.Persistence.Models;
using Microsoft.Data.Sqlite;

namespace ISO11820.App.Features.History;

public sealed class HistoryCoordinator
{
    private readonly DbHelper _dbHelper;

    public HistoryCoordinator(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<Operator> QueryOperators()
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = "SELECT id, username, password, role, created_at FROM operators ORDER BY id";

        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var result = new List<Operator>();
        while (reader.Read())
        {
            result.Add(new Operator
            {
                Id = reader.GetInt64(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2),
                Role = reader.GetString(3),
                CreatedAt = reader.GetString(4)
            });
        }

        return result;
    }

    public List<ProductMaster> QueryProducts(string? productCode = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = productCode is null
            ? "SELECT id, product_code, product_name, description, created_at FROM productmaster ORDER BY id"
            : "SELECT id, product_code, product_name, description, created_at FROM productmaster WHERE product_code = @code ORDER BY id";

        using var command = new SqliteCommand(sql, connection);
        if (productCode is not null)
        {
            command.Parameters.AddWithValue("@code", productCode);
        }

        using var reader = command.ExecuteReader();

        var result = new List<ProductMaster>();
        while (reader.Read())
        {
            result.Add(new ProductMaster
            {
                Id = reader.GetInt64(0),
                ProductCode = reader.GetString(1),
                ProductName = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetString(4)
            });
        }

        return result;
    }

    public List<TestMaster> QueryTestTypes(string? testType = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = testType is null
            ? "SELECT id, test_type, description, standard, created_at FROM testmaster ORDER BY id"
            : "SELECT id, test_type, description, standard, created_at FROM testmaster WHERE test_type = @type ORDER BY id";

        using var command = new SqliteCommand(sql, connection);
        if (testType is not null)
        {
            command.Parameters.AddWithValue("@type", testType);
        }

        using var reader = command.ExecuteReader();

        var result = new List<TestMaster>();
        while (reader.Read())
        {
            result.Add(new TestMaster
            {
                Id = reader.GetInt64(0),
                TestType = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Standard = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetString(4)
            });
        }

        return result;
    }
}