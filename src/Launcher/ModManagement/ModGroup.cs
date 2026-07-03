using AxiomPlayground.Modding;

namespace Launcher.ModManagement;

public sealed class ModGroup(string modId)
{
    public string ModId { get; } = modId;

    private readonly List<Mod> _mods = [];
    public IReadOnlyList<Mod> Mods => _mods;

    public bool HasDuplicates => _mods.Count > 1;

    // NEW: group-level enable state (checkbox)
    public bool IsEnabled { get; set; } = true;

    // NEW: selected source (radio group)
    public ModSource SelectedSource { get; private set; }

    public void Add(Mod mod)
    {
        ArgumentNullException.ThrowIfNull(mod);

        if (!mod.Info.Id.Equals(ModId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                Shared.T("errorModGroupCannotAdd", mod.Info.Id, ModId));
        }

        _mods.Add(mod);
    }

    /// <summary>
    /// Must be called after all mods are added.
    /// Uses ModSourcePolicy to pick default selection.
    /// </summary>
    public void ResolveSelection(ModSelectionState? state = null)
    {
        var orderedSources = ModSourcePolicy.Sort(
            _mods.Select(m => m.Source).Distinct()
        ).ToList();

        if (orderedSources.Count == 0)
            throw new InvalidOperationException(Shared.T("errorModGroupNoSourceInGroup", ModId));

        if (state != null && orderedSources.Contains(state.SelectedSource))
        {
            SelectedSource = state.SelectedSource;
        }
        else
        {
            SelectedSource = orderedSources[0];
        }
    }

    public void SetSelectedSource(ModSource source)
    {
        if (!_mods.Any(m => m.Source == source))
            throw new ArgumentException(Shared.T("errorModGroupInvalidSource", ModId, source));

        SelectedSource = source;
    }

    public Mod GetSelectedMod()
    {
        return _mods.First(m => m.Source == SelectedSource);
    }
}