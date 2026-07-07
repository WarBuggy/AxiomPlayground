using AxiomPlayground.Modding.Discovery;
using Launcher.ModManagement;

namespace Launcher;

public partial class Launcher : Form
{
    private List<ModGroup> _groups = [];
    private Dictionary<string, ModGroup> _groupLookup =
        new(StringComparer.OrdinalIgnoreCase);

    public Launcher()
    {
        InitializeComponent();

        Shown += Launcher_Shown;

        _selectedListControl.EntryRemoveRequest += ModRemoveRequested;
        _availableListControl.OnModSelectionChanged += ModSelectionChanged;
    }

    private void Launcher_Shown(object? sender, EventArgs e)
    {
        var discovery = new ModDiscovery();

        var mods = discovery.Discover();

        _groups = ModGroupBuilder.Build(mods);
        ValidateCoreExists(_groups);
        _groups = ModGroupOrdering.Order(_groups);

        var stateLookup = ModSelectionStore.Load();
        foreach (var group in _groups)
        {
            stateLookup.TryGetValue(group.ModId, out var state);
            group.ResolveSelectionState(state);
        }

        // NOTE: must be rebuilt if mod list changes
        _groupLookup =
            _groups.ToDictionary(group => group.ModId, StringComparer.OrdinalIgnoreCase);

        RenderModGroups(_groups);

        foreach (var group in _groups)
        {
            if (!group.IsEnabled)
                continue;

            ApplyModSelection(group.ModId, group.IsEnabled, true);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        var states = new Dictionary<string, ModSelectionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in _groups)
        {
            states[group.ModId] = new ModSelectionState
            {
                ModId = group.ModId,
                SelectedSource = group.SelectedSource,
                IsEnabled = group.IsEnabled
            };
        }

        ModSelectionStore.Save(states);
    }

    private void RenderModGroups(IEnumerable<ModGroup> groups)
    {
        _availableListControl.Bind(groups);
    }

    private static void ValidateCoreExists(IEnumerable<ModGroup> groups)
    {
        if (!groups.Any(g => g.IsCore))
        {
            throw new InvalidOperationException(
                Shared.T("errorLauncherNoCore", ModSystemPolicy.CORE_MOD_ID));
        }
    }

    private void ModRemoveRequested(string modId)
    {
        ApplyModSelection(modId, false);
    }

    private void ModSelectionChanged(string modId, bool isEnabled)
    {
        ApplyModSelection(modId, isEnabled);
    }

    private void ApplyModSelection(string modId, bool enabled, bool forced = false)
    {
        if (!_groupLookup.TryGetValue(modId, out var group))
            return;

        if (!enabled && !group.CanBeDisabled)
            return;

        if (group.IsEnabled == enabled && !forced)
            return;

        group.IsEnabled = enabled;

        _availableListControl.SetStatusForEntryOfGroup(group.ModId);

        if (enabled)
            _selectedListControl.Add(group);
        else
            _selectedListControl.Remove(group.ModId);
    }
}