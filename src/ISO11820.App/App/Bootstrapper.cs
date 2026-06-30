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
using OfficeOpenXml;
using Serilog;

namespace ISO11820.App.App;

public static class Bootstrapper
{
    public static Iso11820AppContext Create()
    {
        // 初始化 Serilog 日志
        AppLogger.Configure();
        Log.Information("ISO 11820 系统启动");

        // 设置 EPPlus 许可证上下文（非商业用途）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var settings = AppSettingsLoader.LoadDefault();
        Log.Information("配置加载完成: Database={Db}, FileStorage={Fs}", settings.Database.SqlitePath, settings.FileStorage.BaseDirectory);

        var dbHelper = new DbHelper(settings.Database.SqlitePath);
        var databaseInitializer = new DatabaseInitializer(dbHelper);
        var csvSampleWriter = new CsvSampleWriter(settings.FileStorage.TestDataDirectory);
        var simulator = new SensorSimulator(settings.Simulation);
        var testController = new TestController(simulator);
        var daqWorker = new DaqWorker(testController);
        var auth = new AuthCoordinator(dbHelper);
        var testExecution = new TestExecutionCoordinator();
        var history = new HistoryCoordinator(dbHelper);
        var calibration = new CalibrationCoordinator(dbHelper);
        var testRecord = new TestRecordCoordinator(csvSampleWriter);

        // 导出服务
        var excelService = new ExcelExportService();
        var pdfService = new PdfExportService();
        var export = new ExportCoordinator(csvSampleWriter, excelService, pdfService);

        databaseInitializer.EnsureCreated();
        Log.Information("数据库初始化完成");

        return new Iso11820AppContext(
            settings,
            dbHelper,
            databaseInitializer,
            csvSampleWriter,
            testController,
            daqWorker,
            auth,
            testExecution,
            history,
            calibration,
            testRecord,
            export);
    }
}
