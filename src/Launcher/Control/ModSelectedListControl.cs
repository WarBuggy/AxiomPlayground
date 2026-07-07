using Launcher.ModManagement;

namespace Launcher.Controls;

public partial class ModSelectedListControl : UserControl
{
    private readonly Dictionary<string, ModSelectedEntryControl> _entries =
        new(StringComparer.OrdinalIgnoreCase);
    public event Action<string>? EntryRemoveRequest;

    public ModSelectedListControl()
    {
        InitializeComponent();
    }

    public void Add(ModSelectedState entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var control = new ModSelectedEntryControl();
        control.Bind(entry);

        control.RemoveRequested += OnEntryRemoveRequested;

        pnlContainer.Controls.Add(control);

        pnlContainer.Controls.SetChildIndex(control, 0);

        _entries.Add(entry.ModId, control);
    }

    private void OnEntryRemoveRequested(string modId)
    {
        EntryRemoveRequest?.Invoke(modId);
    }

    public void Remove(string modId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);

        if (!_entries.TryGetValue(modId, out var control))
            return;

        control.RemoveRequested -= OnEntryRemoveRequested;

        pnlContainer.Controls.Remove(control);
        _entries.Remove(modId);

        control.Dispose();
    }

    public void Add(ModGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (_entries.ContainsKey(group.ModId))
            return;

        var entry = new ModSelectedState
        {
            ModId = group.ModId,
            Source = group.SelectedSource,
            CanRemoveFromSelection = group.CanRemoveFromSelection,
        };

        var control = new ModSelectedEntryControl();
        control.Bind(entry);

        control.RemoveRequested += OnEntryRemoveRequested;

        pnlContainer.Controls.Add(control);
        pnlContainer.Controls.SetChildIndex(control, 0);

        _entries.Add(group.ModId, control);
    }

    public IReadOnlyList<ModSelectedState> GetSelectedModsInDisplayOrder()
    {
        return [.. pnlContainer.Controls
            .OfType<ModSelectedEntryControl>()
            .Select(c => c.Entry)];
    }
}