namespace Launcher;

using System.Globalization;
using System.Reflection;
using System.Resources;
using global::Launcher.Config;

partial class Launcher
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuMain;
    private Panel panelLeft;
    private Panel panelRight;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(ConfigManager.Launcher.WindowWidth, ConfigManager.Launcher.WindowHeight);
        StartPosition = FormStartPosition.CenterScreen;
        if (ConfigManager.Launcher.StartMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        menuMain = new MenuStrip();
        panelLeft = new Panel();
        var panelDivider = new Panel();
        panelRight = new Panel();

        panelLeft.Dock = DockStyle.Left;
        panelLeft.Width = ConfigManager.Launcher.LeftPanelWidth;

        panelDivider.Dock = DockStyle.Left;
        panelDivider.Width = 1;
        panelDivider.BackColor = Color.LightGray;

        panelRight.Dock = DockStyle.Fill;

        Controls.Add(panelRight);
        Controls.Add(panelDivider);
        Controls.Add(panelLeft);
        Controls.Add(menuMain);

        MainMenuStrip = menuMain;

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Text = Shared.T("formTitle");
    }
}