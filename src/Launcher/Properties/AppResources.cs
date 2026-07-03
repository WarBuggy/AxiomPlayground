using System.Resources;

namespace Launcher.Properties;

public static class AppResources
{
    private static readonly ResourceManager RM =
        new("Launcher.Properties.Resources", typeof(AppResources).Assembly);

    public static Image ExpandIcon => (Image)RM.GetObject("ExpandIcon")!;
    public static Image SteamIcon => (Image)RM.GetObject("SteamIcon")!;
    public static Image LocalIcon => (Image)RM.GetObject("LocalIcon")!;
}