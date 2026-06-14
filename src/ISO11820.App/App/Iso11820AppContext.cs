using ISO11820.App.Config;
using ISO11820.App.Infrastructure.FileStorage;
using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Runtime.Controller;
using ISO11820.App.Runtime.Services;

namespace ISO11820.App.App;

public sealed class Iso11820AppContext
{
    public Iso11820AppContext(
        AppSettings settings,
        DbHelper dbHelper,
        DatabaseInitializer databaseInitializer,
        CsvSampleWriter csvSampleWriter,
        TestController testController,
        DaqWorker daqWorker)
    {
        Settings = settings;
        DbHelper = dbHelper;
        DatabaseInitializer = databaseInitializer;
        CsvSampleWriter = csvSampleWriter;
        TestController = testController;
        DaqWorker = daqWorker;
    }

    public AppSettings Settings { get; }

    public DbHelper DbHelper { get; }

    public DatabaseInitializer DatabaseInitializer { get; }

    public CsvSampleWriter CsvSampleWriter { get; }

    public TestController TestController { get; }

    public DaqWorker DaqWorker { get; }
}
