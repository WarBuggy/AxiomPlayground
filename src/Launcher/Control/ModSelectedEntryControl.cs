using AxiomPlayground.Shared;
using Launcher.ModManagement;
using Launcher.Properties;

namespace Launcher.Controls;

public partial class ModSelectedEntryControl : UserControl
{
    private ModSelectedState _entry = null!;
    public event Action<string>? RemoveRequested;

    public ModSelectedState Entry => _entry;

    public ModSelectedEntryControl()
    {
        InitializeComponent();
    }

    public void Bind(ModSelectedState entry)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));

        lblModId.Text = entry.ModId;
        picSource.Image = ModSourcePolicy.GetSourceIcon(entry.Source);

        ApplyEntryPolicy();
    }

    private void ApplyEntryPolicy()
    {
        bool canRemove = _entry.CanRemoveFromSelection;

        picRemove.Enabled = canRemove;
        picRemove.Image = canRemove
            ? AppResources.RemoveIcon
            : null;

        picRemove.Click -= PicRemove_Click;

        if (canRemove)
        {
            picRemove.Click += PicRemove_Click;
        }
    }

    private void PicRemove_Click(object? sender, EventArgs e)
    {
        RemoveRequested?.Invoke(_entry.ModId);
    }
}