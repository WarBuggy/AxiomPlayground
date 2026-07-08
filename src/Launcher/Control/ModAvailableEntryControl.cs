using AxiomPlayground.Modding;
using Launcher.ModManagement;
using Launcher.Properties;

namespace Launcher.Controls;

public partial class ModAvailableEntryControl : UserControl
{
    private ModGroup _group = null!;
    public event Action<string, bool>? SelectionChanged;

    public ModAvailableEntryControl()
    {
        InitializeComponent();
    }

    public void Bind(ModGroup group)
    {
        _group = group ?? throw new ArgumentNullException(nameof(group));

        lblModName.Text = group.ModId;

        chkEnabled.CheckedChanged -= ChkEnabled_CheckedChanged;
        chkEnabled.Checked = group.IsEnabled;
        chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;

        UpdateSourceIcon();

        ApplyGroupPolicy();
        ApplyActiveState();
    }

    private void UpdateSourceIcon()
    {
        picSource.Image = ModSourceIconCache.Get(
            _group.SelectedSource,
            _group.IsEnabled);
    }

    private void ChkEnabled_CheckedChanged(object? sender, EventArgs e)
    {
        SelectionChanged?.Invoke(
            _group.ModId,
            chkEnabled.Checked);
    }

    private void PicExpand_Click(object? sender, EventArgs e)
    {
        BuildDropdown();

        var screenPoint = picExpand.PointToScreen(
            new Point(0, picExpand.Height));

        _dropdown.Show(screenPoint);
    }

    private void BuildDropdown()
    {
        _dropdown.Items.Clear();

        foreach (var mod in _group.Mods)
        {
            var item = new ToolStripMenuItem(mod.Source.ToString())
            {
                Checked = mod.Source == _group.SelectedSource,
                Image = ModSourcePolicy.GetSourceIcon(mod.Source),
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                Margin = new Padding(0, LauncherLayout.TinySpacingSize, LauncherLayout.SpacingSize, 0),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };

            item.Click += (_, _) =>
            {
                _group.SetSelectedSource(mod.Source);

                UpdateSourceIcon();
                BuildDropdown();
            };

            _dropdown.Items.Add(item);
        }
    }

    public void SetActiveState()
    {
        bool isEnabled = _group.IsEnabled;

        chkEnabled.CheckedChanged -= ChkEnabled_CheckedChanged;
        chkEnabled.Checked = isEnabled;
        chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;

        ApplyActiveState();
    }

    private void ApplyActiveState()
    {
        bool isEnabled = _group.IsEnabled;

        picModImage.Enabled = !isEnabled;
        picModImage.BackColor = isEnabled
            ? Color.Gray
            : Color.Aquamarine;

        lblModName.ForeColor = isEnabled
            ? Color.Gray
            : SystemColors.ControlText;

        picSource.Enabled = !isEnabled;
        picSource.Image = ModSourceIconCache.Get(_group.SelectedSource, isEnabled);

        UpdateExpandState();
    }

    public void ApplyGroupPolicy()
    {
        chkEnabled.CheckedChanged -= ChkEnabled_CheckedChanged;

        chkEnabled.Enabled = !_group.IsCore;

        if (_group.IsCore)
        {
            chkEnabled.Checked = true;
        }
        else
        {
            chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
        }
    }

    private void UpdateExpandState()
    {
        bool canExpand =
            !_group.IsEnabled &&
            !_group.IsLocalOnly &&
            _group.HasMultipleSources;

        picExpand.Image = canExpand
            ? AppResources.ExpandIcon
            : AppResources.BlankIcon;

        picExpand.Enabled = canExpand;

        picExpand.Click -= PicExpand_Click;

        if (canExpand)
        {
            picExpand.Click += PicExpand_Click;
        }
    }

    public void SetSelectedSource(ModSource source)
    {
        _group.SetSelectedSource(source);

        UpdateSourceIcon();
    }
}