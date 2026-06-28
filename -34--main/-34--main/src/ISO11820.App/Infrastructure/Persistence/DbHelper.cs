namespace ISO11820.App.Infrastructure.Persistence;

public sealed class DbHelper
{
    public DbHelper(string sqlitePath)
    {
        SqlitePath = sqlitePath;
    }

    public string SqlitePath { get; }
}
