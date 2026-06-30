using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace ISO11820.App.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly DbHelper _dbHelper;

    public DatabaseInitializer(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public void EnsureCreated()
    {
        EnsureDirectory();
        CreateTables();
        SeedData();
    }

    private void EnsureDirectory()
    {
        var directory = Path.GetDirectoryName(_dbHelper.SqlitePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void CreateTables()
    {
        using var connection = _dbHelper.CreateConnection();

        var sql = @"
            CREATE TABLE IF NOT EXISTS operators (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                pwd TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'experimenter',
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS apparatus (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                model TEXT,
                serial_number TEXT,
                serial_port_config TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS productmaster (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_code TEXT NOT NULL,
                test_id TEXT,
                product_name TEXT NOT NULL,
                specification TEXT,
                height_mm REAL,
                diameter_mm REAL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                UNIQUE(product_code, test_id)
            );

            CREATE TABLE IF NOT EXISTS testmaster (
                productid TEXT NOT NULL,
                testid TEXT NOT NULL,
                testdate TEXT NOT NULL DEFAULT (date('now')),
                operator TEXT,
                sample_name TEXT,
                specification TEXT,
                height_mm REAL,
                diameter_mm REAL,
                preweight REAL,
                postweight REAL,
                lostweight_per REAL,
                deltatf REAL,
                totaltesttime INTEGER,
                flame_time INTEGER,
                flame_duration INTEGER,
                has_flame INTEGER DEFAULT 0,
                env_temp REAL,
                env_humidity REAL,
                notes TEXT,
                flag TEXT DEFAULT '00000000',
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                PRIMARY KEY (productid, testid)
            );

            CREATE TABLE IF NOT EXISTS sensors (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                channel INTEGER,
                range_scale TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS ""CalibrationRecords"" (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sensor_id INTEGER NOT NULL,
                calibration_date TEXT NOT NULL,
                result_json TEXT,
                technician TEXT,
                notes TEXT,
                operator TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );
        ";

        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private void SeedData()
    {
        using var connection = _dbHelper.CreateConnection();

        SeedOperators(connection);
        SeedApparatus(connection);
        SeedSensors(connection);
    }

    private static void SeedOperators(SqliteConnection connection)
    {
        var adminHash = HashPassword("123456");
        var experimenterHash = HashPassword("123456");

        var sql = @"
            INSERT OR IGNORE INTO operators (username, pwd, role)
            VALUES ('admin', @adminPwd, 'admin');

            INSERT OR IGNORE INTO operators (username, pwd, role)
            VALUES ('experimenter', @expPwd, 'experimenter');
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@adminPwd", adminHash);
        command.Parameters.AddWithValue("@expPwd", experimenterHash);
        command.ExecuteNonQuery();
    }

    private static void SeedApparatus(SqliteConnection connection)
    {
        var sql = @"
            INSERT OR IGNORE INTO apparatus (id, name, model, serial_number, serial_port_config)
            VALUES (1, 'ISO11820 Test Furnace', 'ISO11820-M1', 'SN-001', 'COM1,9600,N,8,1');
        ";

        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private static void SeedSensors(SqliteConnection connection)
    {
        var sql = @"
            INSERT OR IGNORE INTO sensors (id, name, type, channel, range_scale)
            VALUES (1, 'Furnace1', 'thermocouple', 1, '0-1200');

            INSERT OR IGNORE INTO sensors (id, name, type, channel, range_scale)
            VALUES (2, 'Furnace2', 'thermocouple', 2, '0-1200');

            INSERT OR IGNORE INTO sensors (id, name, type, channel, range_scale)
            VALUES (3, 'Surface', 'thermocouple', 3, '0-800');

            INSERT OR IGNORE INTO sensors (id, name, type, channel, range_scale)
            VALUES (4, 'Center', 'thermocouple', 4, '0-800');

            INSERT OR IGNORE INTO sensors (id, name, type, channel, range_scale)
            VALUES (5, 'Calibration', 'reference', 5, '0-1200');
        ";

        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    public bool ValidateLogin(string username, string password)
    {
        var hash = HashPassword(password);

        using var connection = _dbHelper.CreateConnection();
        var sql = "SELECT COUNT(1) FROM operators WHERE username = @username AND pwd = @password";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", hash);

        var result = command.ExecuteScalar();
        return result is long count && count > 0;
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}