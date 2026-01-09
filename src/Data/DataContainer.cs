using System.Text.Json;

namespace AxiomPlayground.Data
{
    /// <summary>
    /// Holds flattened JSON data for a single mod and resolves same-path conflicts.
    /// </summary>
    public sealed class DataContainer
    {
        private readonly Dictionary<string, object> _flatData = new(StringComparer.OrdinalIgnoreCase);

        public DataContainer() { }

        public void AddToFlatData(JsonElement root, string? samePathConflict = null, object? samePathConflictArray = null)
        {
            Flatten(root, "", _flatData, samePathConflict, samePathConflictArray);
        }

        private static void Flatten(JsonElement element, string currentPath, Dictionary<string, object> output, string? samePathConflict, object? samePathConflictArray)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var nextPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";
                        Flatten(prop.Value, nextPath, output, samePathConflict, samePathConflictArray);
                    }
                    break;

                case JsonValueKind.Array:
                    HandleLeaf(output, currentPath, ExtractArray(element), samePathConflict, samePathConflictArray);
                    break;

                default:
                    var value = ExtractPrimitive(element);
                    if (value != null) // null leafs are ignored
                        HandleLeaf(output, currentPath, value, samePathConflict, samePathConflictArray);
                    break;
            }
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

        private static void HandleLeaf(Dictionary<string, object> output, string path, object value, string? samePathConflict, object? samePathConflictArray)
        {
            // If incoming value is null, it is ignored
            if (value == null)
                return;

            bool exists = output.TryGetValue(path, out var existing);
            if (!exists)
            {
                output[path] = value;
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
                    RemoveConflictingKeys(output, path);
                    return;
                }

                if (existing == null)
                    throw new InvalidOperationException($"[DataContainer] Existing value of {path} is unexpectedly null.");

                int? index = SamePathConflictArrayToInt(samePathConflictArray);
                if (index is int i)
                {
                    var existingList = existingIsArray ? (List<object>)existing : [existing];
                    var incomingList = incomingIsArray ? (List<object>)value : [value];

                    if (i < 0 || i > existingList.Count)
                        i = existingList.Count;

                    existingList.InsertRange(i, incomingList);
                    output[path] = existingList;
                    RemoveConflictingKeys(output, path);
                }

                // default: ignore
                return;
            }

            if (IsOverwrite(samePathConflict))
            {
                output[path] = value;
                RemoveConflictingKeys(output, path);
            }
            // default: ignore
        }

        private static void RemoveConflictingKeys(Dictionary<string, object> output, string path)
        {
            // Remove any existing keys that are prefixes of the incoming path
            var keysToRemove = output.Keys.Where(k => path.StartsWith(k + ".", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var k in keysToRemove)
                output.Remove(k);

            // Remove parent keys up to second last segment
            var segments = path.Split('.');
            if (segments.Length <= 1) return;

            string prefix = "";
            for (int i = 0; i < segments.Length - 1; i++)
            {
                prefix = i == 0 ? segments[i] : prefix + "." + segments[i];
                output.Remove(prefix);
            }
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

        /// <summary>
        /// Gets a value from the flattened data by path.
        /// Returns null if the path does not exist.
        /// </summary>
        public object? GetFlatData(string path)
        {
            _flatData.TryGetValue(path, out var value);
            return value;
        }

        public void SetFlatData(string path, object value)
        {
            _flatData[path] = value;
        }

        // =========================
        // Debug
        // =========================

        public void PrintDebug()
        {
            Console.WriteLine("---- Flattened Data ----");
            foreach (var kv in _flatData.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{kv.Key} = {FormatValue(kv.Value)}");
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
}
