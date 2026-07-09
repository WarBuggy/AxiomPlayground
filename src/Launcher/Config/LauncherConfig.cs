using System.Security.Permissions;

namespace Launcher.Config;

public class LauncherConfig : BaseConfig
{
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
    public int LeftPanelWidth { get; set; } = 250;
    public int AvailablePanelWidth { get; set; } = 400;
    public bool StartMaximized { get; set; } = false;
    public string ExecutableFile { get; set; } = "in254.exe";
    public override string GetSectionName() => "launcher";
}