using Launcher.Properties;

namespace Launcher.Controls
{
    partial class ModGroupControl
    {
        private const int RowHeight = 24;
        private const int IconSize = 16;
        private const int SpacingSize = 8;
        private const int TinySpacingSize = 2;
        private const float DefaultFontSize = 10F;
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel pnlHeader;
        private Label lblModName;
        private PictureBox picSource;
        private PictureBox picExpand;
        private readonly ToolStripDropDown _dropdown = new ToolStripDropDown();
        private Panel rootPanel;
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
            rootPanel = new Panel();
            pnlHeader = new TableLayoutPanel();
            lblModName = new Label();
            picSource = new PictureBox();
            picExpand = new PictureBox();
            pnlBottomBorder = new Panel(); SuspendLayout();

            // rootPanel 
            rootPanel.Dock = DockStyle.Fill;

            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.AutoSize = true;
            pnlHeader.ColumnCount = 3;
            pnlHeader.RowCount = 1;
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // lblModName 
            lblModName.AutoSize = true;
            lblModName.Font = new Font("Segoe UI", DefaultFontSize, FontStyle.Bold);
            lblModName.Padding = new Padding(SpacingSize, SpacingSize, SpacingSize, SpacingSize);

            // picExpand
            picExpand.SizeMode = PictureBoxSizeMode.CenterImage;
            picExpand.Size = new Size(IconSize, IconSize);
            picExpand.Margin = new Padding(0, 0, TinySpacingSize, 0);
            picExpand.Anchor = AnchorStyles.Right;
            picExpand.SizeMode = PictureBoxSizeMode.Zoom;
            picExpand.Cursor = Cursors.Hand;
            picExpand.Image = AppResources.ExpandIcon;

            // picSource
            picSource.SizeMode = PictureBoxSizeMode.CenterImage;
            picSource.Size = new Size(IconSize, IconSize);
            picSource.Margin = new Padding(0, 0, TinySpacingSize, 0);
            picSource.Anchor = AnchorStyles.Right;
            picSource.SizeMode = PictureBoxSizeMode.Zoom;

            pnlHeader.Controls.Add(lblModName, 0, 0);
            pnlHeader.Controls.Add(picSource, 1, 0);
            pnlHeader.Controls.Add(picExpand, 2, 0);

            pnlBottomBorder.Dock = DockStyle.Bottom;
            pnlBottomBorder.Height = 2;
            pnlBottomBorder.BackColor = Color.LightGray;

            rootPanel.Controls.Add(pnlHeader);
            rootPanel.Controls.Add(pnlBottomBorder);

            Controls.Add(rootPanel); ResumeLayout(false);
        }
    }
}