using System.Collections;
using System.Diagnostics.Tracing;
using System.Windows.Forms.VisualStyles;
using Launcher.Properties;

namespace Launcher.Controls;

partial class ModAvailableEntryControl
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel pnlHeader;
    private Label lblModName;
    private PictureBox picSource;
    private PictureBox picExpand;
    private readonly ToolStripDropDown _dropdown = new ToolStripDropDown();
    private Panel pnlBottomBorder;
    private CheckBox chkEnabled;
    private TableLayoutPanel pnlRow;
    private PictureBox picModImage;

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

        pnlRow = new TableLayoutPanel();
        chkEnabled = new CheckBox();
        picModImage = new PictureBox();
        pnlHeader = new TableLayoutPanel();
        lblModName = new Label();
        picSource = new PictureBox();
        picExpand = new PictureBox();
        pnlBottomBorder = new Panel();

        SuspendLayout();

        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = Padding.Empty;
        Margin = Padding.Empty;

        //
        // pnlRow
        //
        pnlRow.Dock = DockStyle.Top;
        pnlRow.AutoSize = true;
        pnlRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pnlRow.ColumnCount = 3;
        pnlRow.RowCount = 1;

        pnlRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        //
        // chkEnabled
        //
        chkEnabled.AutoSize = true;
        chkEnabled.Margin = new Padding(LauncherLayout.SpacingSize, LauncherLayout.TinySpacingSize,
            LauncherLayout.SpacingSize, LauncherLayout.TinySpacingSize);
        chkEnabled.Anchor = AnchorStyles.Left;

        //
        // picModImage
        //
        picModImage.Size = new Size(LauncherLayout.ThumbnailSize, LauncherLayout.ThumbnailSize);
        picModImage.SizeMode = PictureBoxSizeMode.Zoom;
        picModImage.BackgroundImageLayout = ImageLayout.Zoom;
        picModImage.BackColor = Color.Aquamarine;

        //
        // pnlHeader
        //
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Margin = Padding.Empty;
        pnlHeader.ColumnCount = 3;
        pnlHeader.RowCount = 1;
        pnlHeader.AutoSize = true;
        pnlHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        //
        // lblModName
        //
        lblModName.AutoSize = true;
        lblModName.Font = new Font(LauncherLayout.DefaultFont, LauncherLayout.DefaultFontSize, FontStyle.Bold);
        lblModName.Padding = new Padding(LauncherLayout.SpacingSize, LauncherLayout.TinySpacingSize,
            LauncherLayout.SpacingSize, 0);

        //
        // picSource
        //
        picSource.Size = new Size(LauncherLayout.IconSize, LauncherLayout.IconSize);
        picSource.SizeMode = PictureBoxSizeMode.Zoom;
        picSource.Margin = new Padding(0, LauncherLayout.TinySpacingSize,
            LauncherLayout.TinySpacingSize, 0);

        picSource.Anchor = AnchorStyles.Top;

        //
        // picExpand
        //
        picExpand.Size = new Size(LauncherLayout.IconSize, LauncherLayout.IconSize);
        picExpand.Margin = new Padding(0, LauncherLayout.TinySpacingSize,
            LauncherLayout.TinySpacingSize, 0);
        picExpand.Anchor = AnchorStyles.Top;
        picExpand.Cursor = Cursors.Hand;
        picExpand.SizeMode = PictureBoxSizeMode.Zoom;

        //
        // Assemble header
        //
        pnlHeader.Controls.Add(lblModName, 0, 0);
        pnlHeader.Controls.Add(picSource, 1, 0);
        pnlHeader.Controls.Add(picExpand, 2, 0);

        //
        // Assemble row
        //
        pnlRow.Controls.Add(chkEnabled, 0, 0);
        pnlRow.Controls.Add(picModImage, 1, 0);
        pnlRow.Controls.Add(pnlHeader, 2, 0);

        //
        // Bottom border
        //
        pnlBottomBorder.Dock = DockStyle.Bottom;
        pnlBottomBorder.Height = 2;
        pnlBottomBorder.BackColor = Color.LightGray;

        Controls.Add(pnlRow);
        Controls.Add(pnlBottomBorder);

        ResumeLayout(false);
    }
}