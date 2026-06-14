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
        var directory = Path.GetDirectoryName(_dbHelper.SqlitePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
