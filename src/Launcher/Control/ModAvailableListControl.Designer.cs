using Launcher.Config;

namespace Launcher.Controls;

partial class ModAvailableListControl
{
    private System.ComponentModel.IContainer components = null;

    private Panel pnlContainer;

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

        pnlContainer = new Panel();

        SuspendLayout();

        pnlContainer.Dock = DockStyle.Fill;
        pnlContainer.AutoScroll = true;
        pnlContainer.Width = ConfigManager.Launcher.AvailablePanelWidth;

        Controls.Add(pnlContainer);

        ResumeLayout(false);
    }
}