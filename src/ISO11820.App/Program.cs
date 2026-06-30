using ISO11820.App.App;
using ISO11820.App.Config;
using ISO11820.App.UI.Forms;

namespace ISO11820.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var appContext = Bootstrapper.Create();
            Application.Run(new MainForm(appContext));
        }
        finally
        {
            AppLogger.CloseAndFlush();
        }
    }
}
