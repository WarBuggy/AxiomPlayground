using AxiomPlayground.Modding;
using Launcher.ModManagement;
using Launcher.Properties;

namespace Launcher.Controls;

public partial class ModGroupControl : UserControl
{
    private ModGroup _group = null!;

    public ModGroupControl()
    {
        InitializeComponent();
        Padding = Padding.Empty;
        Margin = Padding.Empty;
    }

    public void Bind(ModGroup group)
    {
        _group = group ?? throw new ArgumentNullException(nameof(group));

        lblModName.Text = group.ModId;

        picExpand.Click += (_, _) =>
        {
            BuildDropdown();
            var screenPoint = picExpand.PointToScreen(new Point(0, picExpand.Height));
            _dropdown.Show(screenPoint);
        };

        bool hasMultipleSources = group.Mods.Count > 1;
        UpdateExpandVisibility(hasMultipleSources);

        UpdateSourceIcon();

        UpdateHeight();
    }

    private void BuildDropdown()
    {
        _dropdown.Items.Clear();

        foreach (var mod in _group.Mods)
        {
            var item = new ToolStripMenuItem(mod.Source.ToString())
            {
                Checked = mod.Source == _group.SelectedSource,
                Image = GetSourceIcon(mod.Source),
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                Padding = new Padding(TinySpacingSize, TinySpacingSize, SpacingSize, TinySpacingSize),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
            };

            item.Click += (_, _) =>
            {
                _group.SetSelectedSource(mod.Source);
                UpdateSourceIcon();
                BuildDropdown(); // refresh check states
            };

            _dropdown.Items.Add(item);
        }
    }

    private static Image GetSourceIcon(ModSource source)
    {
        return source switch
        {
            ModSource.Steam => AppResources.SteamIcon,
            ModSource.Local => AppResources.LocalIcon,
            _ => AppResources.LocalIcon
        };
    }

    private void UpdateHeight()
    {
        Height = pnlHeader.Height + pnlBottomBorder.Height;
    }

    private void UpdateExpandVisibility(bool hasMultipleSources)
    {
        picExpand.Image = hasMultipleSources ? AppResources.ExpandIcon : null;
        picExpand.Enabled = hasMultipleSources;
    }

    private void UpdateSourceIcon()
    {
        picSource.Image = GetSourceIcon(_group.SelectedSource);
    }
}