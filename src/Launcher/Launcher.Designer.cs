namespace Launcher;

using global::Launcher.Config;

partial class Launcher
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuMain;

    // Main layout
    private Panel panelLeft;
    private Panel panelWorking;

    // Working area
    private Panel panelAvailableMods;
    private Panel panelSelectionButtons;
    private Panel panelSelectedMods;

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
        ClientSize = new Size(
            ConfigManager.Launcher.WindowWidth,
            ConfigManager.Launcher.WindowHeight);

        StartPosition = FormStartPosition.CenterScreen;

        if (ConfigManager.Launcher.StartMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        menuMain = new MenuStrip();

        panelLeft = new Panel();
        panelWorking = new Panel();

        panelAvailableMods = new Panel();
        panelSelectionButtons = new Panel();
        panelSelectedMods = new Panel();

        var panelMainDivider = new Panel();
        var panelButtonDividerLeft = new Panel();
        var panelButtonDividerRight = new Panel();

        //
        // panelLeft
        //
        panelLeft.Dock = DockStyle.Left;
        panelLeft.Width = ConfigManager.Launcher.LeftPanelWidth;

        //
        // panelMainDivider
        //
        panelMainDivider.Dock = DockStyle.Left;
        panelMainDivider.Width = 1;
        panelMainDivider.BackColor = Color.LightGray;

        //
        // panelWorking
        //
        panelWorking.Dock = DockStyle.Fill;

        //
        // panelAvailableMods
        //
        panelAvailableMods.Dock = DockStyle.Left;
        panelAvailableMods.AutoScroll = true;
        panelAvailableMods.Width = 450;

        //
        // panelButtonDividerLeft
        //
        panelButtonDividerLeft.Dock = DockStyle.Left;
        panelButtonDividerLeft.Width = 1;
        panelButtonDividerLeft.BackColor = Color.LightGray;

        //
        // panelSelectionButtons
        //
        panelSelectionButtons.Dock = DockStyle.Left;
        panelSelectionButtons.Width = 80;

        //
        // panelButtonDividerRight
        //
        panelButtonDividerRight.Dock = DockStyle.Left;
        panelButtonDividerRight.Width = 1;
        panelButtonDividerRight.BackColor = Color.LightGray;

        //
        // panelSelectedMods
        //
        panelSelectedMods.Dock = DockStyle.Fill;

        //
        // Build working area
        //
        panelWorking.Controls.Add(panelSelectedMods);
        panelWorking.Controls.Add(panelButtonDividerRight);
        panelWorking.Controls.Add(panelSelectionButtons);
        panelWorking.Controls.Add(panelButtonDividerLeft);
        panelWorking.Controls.Add(panelAvailableMods);

        //
        // Build main window
        //
        Controls.Add(panelWorking);
        Controls.Add(panelMainDivider);
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