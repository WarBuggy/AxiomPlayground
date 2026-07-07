namespace Launcher.Controls;

partial class ModSelectedListControl
{
    private System.ComponentModel.Container? components = null;

    private Panel pnlContainer = null!;

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

        pnlContainer.Dock = DockStyle.Left;
        pnlContainer.AutoScroll = true;
        pnlContainer.Width = 300;

        Controls.Add(pnlContainer);

        ResumeLayout(false);
    }
}