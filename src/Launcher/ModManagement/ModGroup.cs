using AxiomPlayground.Shared;
using AxiomPlayground.Modding;

namespace Launcher.ModManagement;

public sealed class ModGroup
{
    public string ModId { get; }
    private readonly List<Mod> _mods = [];
    public IReadOnlyList<Mod> Mods => _mods;
    public bool HasMultipleSources => _mods.Count > 1;
    public ModSource SelectedSource { get; private set; }
    public bool IsCore;
    private bool _isEnabled;
    public bool IsLocalOnly;
    public bool CanBeDisabled;
    public bool CanRemoveFromSelection;


    public ModGroup(string modId)
    {
        ModId = modId;
        IsCore =
            ModId.Equals(ModSystemPolicy.CORE_MOD_ID, StringComparison.OrdinalIgnoreCase);
        _isEnabled = IsCore;
        IsLocalOnly = IsCore;
        CanBeDisabled = !IsCore;
        CanRemoveFromSelection = !IsCore;
    }

    public void Add(Mod mod)
    {
        ArgumentNullException.ThrowIfNull(mod);

        if (!mod.Info.Id.Equals(ModId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                Shared.T("errorModGroupCannotAdd", mod.Info.Id, ModId));

        if (IsLocalOnly && mod.Source != ModSource.Local)
            throw new ArgumentException(
                Shared.T("errorModGroupLocalOnly", ModId));

        if (_mods.Any(m => m.Source == mod.Source))
            throw new ArgumentException(
                Shared.T("errorModGroupDuplicateSource", ModId, mod.Source));

        _mods.Add(mod);
    }

    public void ResolveSelectionState(ModSelectionState? state = null)
    {
        var orderedSources =
            ModSourcePolicy.Sort(_mods.Select(m => m.Source).Distinct()).ToList();

        if (orderedSources.Count == 0)
            throw new InvalidOperationException(
                Shared.T("errorModGroupNoSourceInGroup", ModId));

        if (IsLocalOnly && orderedSources.Contains(ModSource.Local))
        {
            SelectedSource = ModSource.Local;
            return;
        }

        if (state != null && orderedSources.Contains(state.SelectedSource))
            SelectedSource = state.SelectedSource;
        else
            SelectedSource = orderedSources[0];
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

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (IsCore && !value)
                return;

            _isEnabled = value;
        }
    }
}