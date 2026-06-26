using ISO11820.App.Config;
using ISO11820.App.Features.Auth;
using ISO11820.App.Features.Calibration;
using ISO11820.App.Features.Export;
using ISO11820.App.Features.History;
using ISO11820.App.Features.TestExecution;
using ISO11820.App.Features.TestRecord;
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
        DaqWorker daqWorker,
        AuthCoordinator auth,
        TestExecutionCoordinator testExecution,
        HistoryCoordinator history,
        CalibrationCoordinator calibration,
        TestRecordCoordinator testRecord,
        ExportCoordinator export)
    {
        Settings = settings;
        DbHelper = dbHelper;
        DatabaseInitializer = databaseInitializer;
        CsvSampleWriter = csvSampleWriter;
        TestController = testController;
        DaqWorker = daqWorker;
        Auth = auth;
        TestExecution = testExecution;
        History = history;
        Calibration = calibration;
        TestRecord = testRecord;
        Export = export;
    }

    public AppSettings Settings { get; }

    public DbHelper DbHelper { get; }

    public DatabaseInitializer DatabaseInitializer { get; }

    public CsvSampleWriter CsvSampleWriter { get; }

    public TestController TestController { get; }

    public DaqWorker DaqWorker { get; }

    public AuthCoordinator Auth { get; }

    public TestExecutionCoordinator TestExecution { get; }

    public HistoryCoordinator History { get; }

    public CalibrationCoordinator Calibration { get; }

    public TestRecordCoordinator TestRecord { get; }

    public ExportCoordinator Export { get; }
}
