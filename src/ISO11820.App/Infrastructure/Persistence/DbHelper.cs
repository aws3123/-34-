using Microsoft.Data.Sqlite;

namespace ISO11820.App.Infrastructure.Persistence;

public sealed class DbHelper
{
    public DbHelper(string sqlitePath)
    {
        SqlitePath = sqlitePath;
    }

    public string SqlitePath { get; }

    public string ConnectionString => $"Data Source={SqlitePath}";

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}