using AxiomPlayground.Modding.Discovery;
using AxiomPlayground.Modding;
using AxiomPlayground.Shared;
using Launcher.ModManagement;
using System.Diagnostics;
using Launcher.Config;

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
        _availableListControl.OnModSelectionChanged += AvailableModSelectionChanged;

        btnPlay.Click += BtnPlay_Click;
    }

    private void Launcher_Shown(object? sender, EventArgs e)
    {
        var mods = ModDiscovery.Discover();

        _groups = ModGroupBuilder.Build(mods);
        ValidateCoreExists(_groups);
        _groups = ModGroupOrdering.Order(_groups);
        BuildGroupLookup();

        RenderModGroups(_groups);

        LoadLauncherState();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        _selectedListControl.EntryRemoveRequest -= ModRemoveRequested;
        _availableListControl.OnModSelectionChanged -= AvailableModSelectionChanged;

        var states = new Dictionary<string, ModSelectionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in _groups)
        {
            states[group.ModId] = new ModSelectionState
            {
                ModId = group.ModId,
                SelectedSource = group.SelectedSource,
            };
        }

        ModSelectionStore.Save(states);
        SaveSelectedModList();
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

    private void AvailableModSelectionChanged(string modId, bool isEnabled)
    {
        ApplyModSelection(modId, isEnabled);
    }

    private void ApplyModSelection(string modId, bool enabled, bool forced = false)
    {
        if (!_groupLookup.TryGetValue(modId, out var group))
            return;

        if (!group.CanBeDisabled && !enabled)
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

    private void BuildGroupLookup()
    {
        _groupLookup =
            _groups.ToDictionary(
                group => group.ModId,
                StringComparer.OrdinalIgnoreCase);
    }

    private void SaveSelectedModList()
    {
        var selectedMods = _selectedListControl.GetSelectedModsInDisplayOrder();

        ModSelectedStore.Save(selectedMods);
    }

    private void ClearSelectedMods()
    {
        foreach (var group in _groups)
        {
            if (!group.IsEnabled)
                continue;

            ApplyModSelection(group.ModId, false);
        }
    }

    private void LoadSelectedModList(List<ModSelectedState> states)
    {
        // Disable all currently enabled mods
        ClearSelectedMods();

        // Restore saved order
        foreach (var selected in states.OrderBy(x => x.Order))
        {
            if (!_groupLookup.TryGetValue(selected.ModId, out var group))
                continue;

            // Restore source
            group.SetSelectedSource(selected.Source);

            _availableListControl.SetSourceForEntryOfGroup(
                selected.ModId,
                selected.Source);

            // Enable mod and add to selected list
            ApplyModSelection(
                selected.ModId,
                true,
                true);
        }
    }

    private void LoadLauncherState()
    {
        var selectionStates = ModSelectionStore.Load();

        var selectedStates = ModSelectedStore.Load();

        var selectedLookup =
            selectedStates.ToDictionary(
                s => s.ModId,
                StringComparer.OrdinalIgnoreCase);

        // First apply saved source preferences.
        foreach (var group in _groups)
        {
            selectionStates.TryGetValue(group.ModId, out var state);

            group.ResolveSelectionState(state);
        }

        // Selected list has priority.
        foreach (var selected in selectedStates)
        {
            if (!_groupLookup.TryGetValue(selected.ModId, out var group))
                continue;

            group.SetSelectedSource(selected.Source);
        }

        // Reflect selected list into ModGroup state.
        foreach (var group in _groups)
        {
            if (selectedLookup.ContainsKey(group.ModId))
                group.IsEnabled = true;
            else
            {
                group.IsEnabled = false;
                ApplyModSelection(group.ModId, group.IsEnabled, true);
            }
        }

        // rebuild selected panel.
        LoadSelectedModList(selectedStates);
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        try
        {
            string gamePath = Path.Combine(AppContext.BaseDirectory,
                ConfigManager.Launcher.ExecutableFile);

            Process.Start(new ProcessStartInfo
            {
                FileName = gamePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Failed to launch game",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}