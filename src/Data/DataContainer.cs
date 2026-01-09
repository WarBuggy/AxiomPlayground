using System.Text.Json;

namespace AxiomPlayground.Data;

/// <summary>
/// Holds flattened JSON data for a single mod and resolves same-path conflicts.
/// Also maintains a category index for manager-level access.
/// </summary>
public sealed class DataContainer
{
    private readonly Dictionary<string, object> _flatData = new(StringComparer.OrdinalIgnoreCase);

    // NEW: category → set of flat paths
    private readonly Dictionary<string, HashSet<string>> _categoryIndex = new(StringComparer.OrdinalIgnoreCase);

    public DataContainer() { }

    public void AddToFlatData(
        JsonElement root,
        string? category = null,
        string? samePathConflict = null,
        object? samePathConflictArray = null)
    {
        Flatten(root, "", category, _flatData, samePathConflict, samePathConflictArray);
    }

    private void Flatten(
        JsonElement element,
        string currentPath,
        string? category,
        Dictionary<string, object> output,
        string? samePathConflict,
        object? samePathConflictArray)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var nextPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";

                    Flatten(prop.Value, nextPath, category, output, samePathConflict, samePathConflictArray);
                }
                break;

            case JsonValueKind.Array:
                HandleLeaf(output, currentPath, ExtractArray(element),
                    category, samePathConflict, samePathConflictArray);
                break;

            default:
                var value = ExtractPrimitive(element);
                if (value != null)
                    HandleLeaf(output, currentPath, value,
                        category, samePathConflict, samePathConflictArray);
                break;
        }
    }

    private void HandleLeaf(
        Dictionary<string, object> output,
        string path,
        object value,
        string? category,
        string? samePathConflict,
        object? samePathConflictArray)
    {
        if (value == null)
            return;

        bool exists = output.TryGetValue(path, out var existing);

        if (!exists)
        {
            output[path] = value;
            RegisterPath(path, category);
            RemoveConflictingKeys(output, path);
            return;
        }

        bool existingIsArray = existing is List<object>;
        bool incomingIsArray = value is List<object>;

        if (existingIsArray || incomingIsArray)
        {
            if (IsOverwrite(samePathConflictArray))
            {
                output[path] = value;
                RegisterPath(path, category);
                RemoveConflictingKeys(output, path);
                return;
            }

            if (existing == null)
                throw new InvalidOperationException(
                    $"[DataContainer] Existing value of {path} is unexpectedly null.");

            int? index = SamePathConflictArrayToInt(samePathConflictArray);
            if (index is int i)
            {
                var existingList = existingIsArray ? (List<object>)existing : new() { existing };
                var incomingList = incomingIsArray ? (List<object>)value : new() { value };

                if (i < 0 || i > existingList.Count)
                    i = existingList.Count;

                existingList.InsertRange(i, incomingList);
                output[path] = existingList;
                RegisterPath(path, category);
                RemoveConflictingKeys(output, path);
            }

            return;
        }

        if (IsOverwrite(samePathConflict))
        {
            output[path] = value;
            RegisterPath(path, category);
            RemoveConflictingKeys(output, path);
        }
    }

    private void RemoveConflictingKeys(Dictionary<string, object> output, string path)
    {
        // Remove children
        var keysToRemove = output.Keys
            .Where(k => path.StartsWith(k + ".", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var k in keysToRemove)
        {
            output.Remove(k);
            UnregisterPath(k);
        }

        // Remove parents
        var segments = path.Split('.');
        if (segments.Length <= 1)
            return;

        string prefix = "";
        for (int i = 0; i < segments.Length - 1; i++)
        {
            prefix = i == 0 ? segments[i] : prefix + "." + segments[i];
            if (output.Remove(prefix))
                UnregisterPath(prefix);
        }
    }

    private void RegisterPath(string path, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        if (!_categoryIndex.TryGetValue(category, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _categoryIndex[category] = set;
        }

        set.Add(path);
    }

    private void UnregisterPath(string path)
    {
        foreach (var set in _categoryIndex.Values)
            set.Remove(path);
    }

    public IReadOnlyCollection<string> GetPathsInCategory(string category)
    {
        return _categoryIndex.TryGetValue(category, out var set)
            ? set
            : Array.Empty<string>();
    }

    private static List<object?> ExtractArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
            list.Add(ExtractPrimitive(item));
        return list;
    }

    private static object? ExtractPrimitive(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element,
            _ => element.ToString()
        };
    }

    private static bool IsOverwrite(object? option)
    {
        if (option is JsonElement e && e.ValueKind == JsonValueKind.String)
            return e.GetString()!.Equals("overwrite", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static int? SamePathConflictArrayToInt(object? samePathConflictArray)
    {
        if (samePathConflictArray is JsonElement e && e.ValueKind == JsonValueKind.Number)
            return (int)e.GetDouble();

        return null;
    }

    public object? GetFlatData(string path)
    {
        _flatData.TryGetValue(path, out var value);
        return value;
    }

    public void SetFlatData(string path, object value)
    {
        _flatData[path] = value;
    }

    public void ClearCategoryIndex(string category)
    {
        _categoryIndex.Remove(category);
    }

    // Debug
    public void PrintDebug()
    {
        Console.WriteLine("---- Flattened Data ----");
        foreach (var kv in _flatData.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{kv.Key} = {FormatValue(kv.Value)}");
        }

        Console.WriteLine("---- Categories ----");
        foreach (var cat in _categoryIndex)
        {
            Console.WriteLine($"{cat.Key}: [{string.Join(", ", cat.Value)}]");
        }

        Console.WriteLine("------------------------");
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is List<object?> list)
            return "[" + string.Join(", ", list.ConvertAll(FormatValue)) + "]";

        return value.ToString()!;
    }
}
