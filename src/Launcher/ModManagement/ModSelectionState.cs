using AxiomPlayground.Modding;

namespace Launcher.ModManagement;

public sealed class ModSelectionState
{
    public string ModId { get; set; } = string.Empty;

    public ModSource SelectedSource { get; set; }
}