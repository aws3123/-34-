using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Infrastructure.Persistence.Models;
using Microsoft.Data.Sqlite;

namespace ISO11820.App.Features.Calibration;

public sealed class CalibrationCoordinator
{
    private readonly DbHelper _dbHelper;

    public CalibrationCoordinator(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public void SaveRecord(CalibrationRecord record)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = @"
            INSERT INTO ""CalibrationRecords"" (sensor_id, calibration_date, result_json, technician, notes)
            VALUES (@sensorId, @calibrationDate, @resultJson, @technician, @notes)
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@sensorId", record.SensorId);
        command.Parameters.AddWithValue("@calibrationDate", record.CalibrationDate);
        command.Parameters.AddWithValue("@resultJson", (object?)record.ResultJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@technician", (object?)record.Technician ?? DBNull.Value);
        command.Parameters.AddWithValue("@notes", (object?)record.Notes ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public List<CalibrationRecord> QueryRecords(long? sensorId = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = sensorId is null
            ? "SELECT id, sensor_id, calibration_date, result_json, technician, notes, created_at FROM \"CalibrationRecords\" ORDER BY calibration_date DESC"
            : "SELECT id, sensor_id, calibration_date, result_json, technician, notes, created_at FROM \"CalibrationRecords\" WHERE sensor_id = @sensorId ORDER BY calibration_date DESC";

        using var command = new SqliteCommand(sql, connection);
        if (sensorId is not null)
        {
            command.Parameters.AddWithValue("@sensorId", sensorId.Value);
        }

        using var reader = command.ExecuteReader();

        var result = new List<CalibrationRecord>();
        while (reader.Read())
        {
            result.Add(new CalibrationRecord
            {
                Id = reader.GetInt64(0),
                SensorId = reader.GetInt64(1),
                CalibrationDate = reader.GetString(2),
                ResultJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                Technician = reader.IsDBNull(4) ? null : reader.GetString(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetString(6)
            });
        }

        return result;
    }
}