using System.Text.Json.Serialization;

namespace AxiomPlayground.Shared;

public class ModSelectedState
{
    public string ModId { get; set; } = string.Empty;
    public ModSource Source { get; set; }
    public int Order { get; set; }

    [JsonIgnore]
    // UI-only state. Not persisted.
    public bool CanRemoveFromSelection { get; set; } = true;
    public ModSelectedState() { }

    public ModSelectedState(
        string modId,
        ModSource source,
        int order = 0,
        bool canRemoveFromSelection = true)
    {
        ModId = modId;
        Source = source;
        Order = order;
        CanRemoveFromSelection = canRemoveFromSelection;
    }
}