namespace Launcher.Controls;

partial class ModSelectedListControl
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlContainer;

    private void InitializeComponent()
    {
        pnlContainer = new Panel();

        SuspendLayout();

        pnlContainer.Dock = DockStyle.Left;
        pnlContainer.AutoScroll = true;
        pnlContainer.Width = 300;

        Controls.Add(pnlContainer);

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }
}