using System.Drawing.Drawing2D;
using Launcher.Properties;

namespace Launcher.Controls;

partial class ModSelectedEntryControl
{
    private const int RemoveIconSize = 24;
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel rootTable;
    private PictureBox picRemove;
    private PictureBox picModIcon;
    private TableLayoutPanel pnlContent;
    private Label lblModId;
    private PictureBox picSource;
    private Panel pnlBottomBorder;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        rootTable = new TableLayoutPanel();
        picRemove = new PictureBox();
        picModIcon = new PictureBox();
        pnlContent = new TableLayoutPanel();
        lblModId = new Label();
        picSource = new PictureBox();
        pnlBottomBorder = new Panel();

        SuspendLayout();

        Dock = DockStyle.Top;
        Padding = Padding.Empty;
        Margin = Padding.Empty;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // rootTable
        rootTable.Dock = DockStyle.Top;
        rootTable.ColumnCount = 3;
        rootTable.RowCount = 1;
        rootTable.AutoSize = true;
        rootTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        rootTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rootTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rootTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // picRemove
        picRemove.Size = new Size(RemoveIconSize, RemoveIconSize);
        picRemove.SizeMode = PictureBoxSizeMode.Zoom;
        picRemove.Cursor = Cursors.Hand;
        picRemove.Image = AppResources.RemoveIcon;
        picRemove.Anchor = AnchorStyles.None;

        // picModIcon
        picModIcon.Size = new Size(LauncherLayout.ThumbnailSize, LauncherLayout.ThumbnailSize);
        picModIcon.SizeMode = PictureBoxSizeMode.Zoom;
        picModIcon.BackColor = Color.Aquamarine;

        lblModId.AutoSize = true;
        lblModId.Dock = DockStyle.Fill;
        lblModId.Font = new Font(LauncherLayout.DefaultFont, LauncherLayout.DefaultFontSize, FontStyle.Bold);
        lblModId.TextAlign = ContentAlignment.MiddleLeft;
        lblModId.Padding = new Padding(LauncherLayout.SpacingSize, LauncherLayout.TinySpacingSize,
            LauncherLayout.SpacingSize, 0);

        picSource.Size = new Size(LauncherLayout.IconSize, LauncherLayout.IconSize);
        picSource.SizeMode = PictureBoxSizeMode.Zoom;
        picSource.Anchor = AnchorStyles.None;
        picSource.Margin = new Padding(0, LauncherLayout.TinySpacingSize, LauncherLayout.SpacingSize, 0);

        // pnlContent
        pnlContent.Dock = DockStyle.Top;
        pnlContent.Margin = Padding.Empty;
        pnlContent.Padding = Padding.Empty;
        pnlContent.ColumnCount = 2;
        pnlContent.RowCount = 1;
        pnlContent.AutoSize = true;
        pnlContent.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        pnlContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlContent.Controls.Add(lblModId, 0, 0);
        pnlContent.Controls.Add(picSource, 1, 0);

        // Assemble
        rootTable.Controls.Add(picRemove, 0, 0);
        rootTable.Controls.Add(picModIcon, 1, 0);
        rootTable.Controls.Add(pnlContent, 2, 0);

        //
        // Bottom border
        //
        pnlBottomBorder.Dock = DockStyle.Bottom;
        pnlBottomBorder.Height = 2;
        pnlBottomBorder.BackColor = Color.LightGray;

        Controls.Add(rootTable);
        Controls.Add(pnlBottomBorder);

        ResumeLayout(false);
    }
}