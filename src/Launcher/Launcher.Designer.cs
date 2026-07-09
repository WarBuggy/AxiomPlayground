using global::Launcher.Config;
using global::Launcher.Controls;
using global::Launcher.Properties;
using AxiomPlayground.Shared;

namespace Launcher;

partial class Launcher
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuMain;

    // Main layout
    private Panel panelLeft;
    private TableLayoutPanel panelWorking;
    // Working area
    private TableLayoutPanel panelControlButtons;
    private FlowLayoutPanel pnlButtonStack;
    private Button btnPlay;
    private Button btnSelect;
    private ModSelectedListControl _selectedListControl = null!;
    private ModAvailableListControl _availableListControl = null!;

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
        panelWorking = new TableLayoutPanel();
        panelControlButtons = new TableLayoutPanel();

        //
        // panelLeft
        //
        panelLeft.Dock = DockStyle.Left;
        panelLeft.Width = ConfigManager.Launcher.LeftPanelWidth;

        //
        // _availableListControl
        //
        _availableListControl = new ModAvailableListControl();
        _availableListControl.Dock = DockStyle.Fill;

        //
        // btnSelect
        //
        btnSelect = new Button();
        btnSelect.Width = 32;
        btnSelect.Height = 32;
        btnSelect.Text = string.Empty;
        btnSelect.BackgroundImage = AppResources.RightArrowIcon;
        btnSelect.BackgroundImageLayout = ImageLayout.Zoom;
        btnSelect.TextImageRelation = TextImageRelation.Overlay;
        btnSelect.FlatStyle = FlatStyle.Flat;
        btnSelect.TabStop = false;
        btnSelect.Cursor = Cursors.Hand;
        btnSelect.FlatAppearance.MouseDownBackColor = Color.LightGray;
        btnSelect.FlatAppearance.MouseOverBackColor = Color.Gainsboro;
        btnSelect.FlatAppearance.BorderSize = 0;
        btnSelect.BackColor = Color.Transparent;

        //
        // pnlButtonStack
        //
        pnlButtonStack = new FlowLayoutPanel();
        pnlButtonStack.FlowDirection = FlowDirection.TopDown;
        pnlButtonStack.WrapContents = false;
        pnlButtonStack.AutoSize = true;
        pnlButtonStack.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pnlButtonStack.Anchor = AnchorStyles.None;
        pnlButtonStack.Dock = DockStyle.None;

        pnlButtonStack.Controls.Add(btnSelect);

        //
        // btnPlay
        //
        btnPlay = new Button();
        btnPlay.Text = "Play";
        btnPlay.Dock = DockStyle.Fill;
        btnPlay.Margin = new Padding(5);

        panelControlButtons.Dock = DockStyle.Left;
        panelControlButtons.Width = 80;
        panelControlButtons.ColumnCount = 1;
        panelControlButtons.RowCount = 3;
        panelControlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelControlButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        panelControlButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panelControlButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        panelControlButtons.Controls.Add(pnlButtonStack, 0, 1);
        panelControlButtons.Controls.Add(btnPlay, 0, 2);

        _selectedListControl = new ModSelectedListControl();
        _selectedListControl.Dock = DockStyle.Fill;

        var panelButtonDividerLeft = new Panel();
        var panelButtonDividerRight = new Panel();
        panelButtonDividerLeft.BackColor = Color.LightGray;
        panelButtonDividerRight.BackColor = Color.LightGray;
        panelButtonDividerLeft.Dock = DockStyle.Fill;
        panelButtonDividerRight.Dock = DockStyle.Fill;

        //
        // panelWorking
        //
        panelWorking.Dock = DockStyle.Fill;
        panelWorking.ColumnCount = 5;
        panelWorking.RowCount = 1;
        panelWorking.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigManager.Launcher.AvailablePanelWidth));
        panelWorking.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));   // divider
        panelWorking.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));  // buttons
        panelWorking.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));   // divider
        panelWorking.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // selected

        panelWorking.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        panelWorking.Controls.Add(_availableListControl, 0, 0);
        panelWorking.Controls.Add(panelButtonDividerLeft, 1, 0);
        panelWorking.Controls.Add(panelControlButtons, 2, 0);
        panelWorking.Controls.Add(panelButtonDividerRight, 3, 0);
        panelWorking.Controls.Add(_selectedListControl, 4, 0);

        //
        // Build main window
        //
        Controls.Add(panelWorking);
        Controls.Add(panelWorking);
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