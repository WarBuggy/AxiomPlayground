using AxiomPlayground.Modding;
using Launcher.ModManagement;

namespace Launcher.Controls;

public partial class ModAvailableListControl : UserControl
{
    public event Action<string, bool>? OnModSelectionChanged;
    private readonly Dictionary<string, ModAvailableEntryControl> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public ModAvailableListControl()
    {
        InitializeComponent();
    }

    public void Bind(IEnumerable<ModGroup> groups)
    {
        pnlContainer.SuspendLayout();
        pnlContainer.Controls.Clear();

        foreach (var group in groups)
        {
            var control = new ModAvailableEntryControl();
            control.Bind(group);
            control.Dock = DockStyle.Top;

            control.SelectionChanged += OnModSelectionChanged;

            pnlContainer.Controls.Add(control);
            pnlContainer.Controls.SetChildIndex(control, 0);

            _entries.Add(group.ModId, control);
        }

        pnlContainer.ResumeLayout();
    }

    public void SetStatusForEntryOfGroup(string modId)
    {
        _entries.TryGetValue(modId, out var entry);
        entry?.SetActiveState();
    }

    public void SetSourceForEntryOfGroup(string modId, ModSource source)
    {
        if (!_entries.TryGetValue(modId, out var entry))
            return;

        entry.SetSelectedSource(source);
    }
}