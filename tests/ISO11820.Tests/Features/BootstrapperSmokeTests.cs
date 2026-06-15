using ISO11820.App.App;

namespace ISO11820.Tests.Features;

public sealed class BootstrapperSmokeTests
{
    [Fact]
    public void Create_Should_Wire_All_Public_Entry_Points()
    {
        var appContext = Bootstrapper.Create();

        Assert.NotNull(appContext.Settings);
        Assert.NotNull(appContext.DbHelper);
        Assert.NotNull(appContext.DatabaseInitializer);
        Assert.NotNull(appContext.CsvSampleWriter);
        Assert.NotNull(appContext.TestController);
        Assert.NotNull(appContext.DaqWorker);
        Assert.NotNull(appContext.Auth);
        Assert.NotNull(appContext.TestExecution);
        Assert.NotNull(appContext.History);
        Assert.NotNull(appContext.Calibration);
        Assert.NotNull(appContext.TestRecord);
        Assert.NotNull(appContext.Export);
    }
}
