using ISO11820.App.Config;
using ISO11820.App.Infrastructure.FileStorage;
using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Runtime.Controller;
using ISO11820.App.Runtime.Services;

namespace ISO11820.App.App;

internal static class Bootstrapper
{
    public static Iso11820AppContext Create()
    {
        var settings = AppSettingsLoader.LoadDefault();
        var dbHelper = new DbHelper(settings.Database.SqlitePath);
        var databaseInitializer = new DatabaseInitializer(dbHelper);
        var csvSampleWriter = new CsvSampleWriter(settings.Output.BaseDirectory);
        var simulator = new SensorSimulator(settings.Simulation);
        var testController = new TestController(simulator);
        var daqWorker = new DaqWorker(testController);

        databaseInitializer.EnsureCreated();

        return new Iso11820AppContext(
            settings,
            dbHelper,
            databaseInitializer,
            csvSampleWriter,
            testController,
            daqWorker);
    }
}
