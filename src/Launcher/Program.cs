namespace Launcher;

using System.Globalization;
using global::Launcher.Config;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ConfigManager.Load();

        //Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");
        ApplicationConfiguration.Initialize();
        Application.Run(new Launcher());

        // ConfigManager.Set<LauncherConfig, int>(nameof(LauncherConfig.WindowHeight), 900);
    }
}