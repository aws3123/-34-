using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Runtime.Controller;
using ISO11820.App.UI.Dialogs;
using ISO11820.Core.Enums;
using Microsoft.Data.Sqlite;

namespace ISO11820.App.Features.TestExecution;

/// <summary>
/// Coordinates the "create new test" workflow between UI and runtime.
/// </summary>
public sealed class TestExecutionCoordinator
{
    /// <summary>
    /// Prepares the runtime for a new test session.
    /// If the controller is in <see cref="TestState.Complete"/> or any non-Idle state,
    /// it is first reset to <see cref="TestState.Idle"/>.
    /// </summary>
    public void PrepareNewTest(TestController controller)
    {
        if (controller.CurrentState != TestState.Idle)
        {
            controller.ResetToIdle();
        }
    }

    /// <summary>
    /// 将试验信息保存到 testmaster 表。
    /// </summary>
    public void SaveTestToDb(TestCreateInfo info, DbHelper dbHelper)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(dbHelper);

        using var connection = dbHelper.CreateConnection();
        var sql = @"INSERT OR REPLACE INTO testmaster
            (productid, testid, testdate, operator, sample_name, specification,
             height_mm, diameter_mm, preweight, env_temp, env_humidity, notes, flag)
            VALUES (@productid, @testid, date('now'), @operator, @sample_name, @specification,
                    @height_mm, @diameter_mm, @preweight, @env_temp, @env_humidity, @notes, '00000000')";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@productid", info.ProductId);
        command.Parameters.AddWithValue("@testid", info.TestId);
        command.Parameters.AddWithValue("@operator", info.OperatorName);
        command.Parameters.AddWithValue("@sample_name", info.SampleName);
        command.Parameters.AddWithValue("@specification", info.Specification);
        command.Parameters.AddWithValue("@height_mm", info.HeightMm);
        command.Parameters.AddWithValue("@diameter_mm", info.DiameterMm);
        command.Parameters.AddWithValue("@preweight", info.PreWeightGrams);
        command.Parameters.AddWithValue("@env_temp", info.EnvTemperature);
        command.Parameters.AddWithValue("@env_humidity", info.EnvHumidity);
        command.Parameters.AddWithValue("@notes", info.Notes);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 将产品信息保存到 productmaster 表。
    /// </summary>
    public void SaveProductToDb(TestCreateInfo info, DbHelper dbHelper)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(dbHelper);

        using var connection = dbHelper.CreateConnection();
        var sql = @"INSERT OR IGNORE INTO productmaster
            (product_code, test_id, product_name, specification, height_mm, diameter_mm)
            VALUES (@product_code, @test_id, @product_name, @specification, @height_mm, @diameter_mm)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@product_code", info.ProductId);
        command.Parameters.AddWithValue("@test_id", info.TestId);
        command.Parameters.AddWithValue("@product_name", info.SampleName);
        command.Parameters.AddWithValue("@specification", info.Specification);
        command.Parameters.AddWithValue("@height_mm", info.HeightMm);
        command.Parameters.AddWithValue("@diameter_mm", info.DiameterMm);
        command.ExecuteNonQuery();
    }
}
