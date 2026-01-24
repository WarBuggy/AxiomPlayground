
namespace AxiomPlayground.Data;

public sealed class CategoryData
(
    string category,
    string modId,
    Dictionary<string, object> values,
    Dictionary<string, PathHistory> history
)
{
    public string Category { get; } = category;
    public string ModId { get; } = modId;
    public IReadOnlyDictionary<string, object> Values { get; } = values;
    public IReadOnlyDictionary<string, PathHistory> History { get; } = history;
}
