using System.Text.Json;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Data;

public sealed class DataManager
{
    private static readonly DataManager _instance = new();
    public static DataManager Instance => _instance;
    private const string DATA_FOLDER = "Data";
    private readonly Dictionary<string, DataContainer> _dataContainers = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HashSet<string> _registeredCategories = new(StringComparer.OrdinalIgnoreCase);
    private DataManager() { }

    public void LoadAll(List<ModInstance> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[DataManager] No mods provided.");

        var validModIds = mods.Select(m => m.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods)
        {
            string dataRoot = Path.Combine(ModManager.Instance.GetModFolderPath(mod), DATA_FOLDER);

            if (!Directory.Exists(dataRoot))
                continue;

            foreach (var jsonFile in Directory.GetFiles(dataRoot, "*.json", SearchOption.AllDirectories))
            {
                JsonDataFile? file;

                try
                {
                    file = JsonSerializer.Deserialize<JsonDataFile>(File.ReadAllText(jsonFile), _jsonOptions);
                }
                catch
                {
                    Console.WriteLine($"[DataManager] Failed to parse '{jsonFile}' in mod '{mod.ModId}'.");
                    continue;
                }

                if (file?.Data == null)
                {
                    Console.WriteLine($"[DataManager] Missing 'data' in '{jsonFile}' (mod '{mod.ModId}').");
                    continue;
                }

                string targetModId =
                    string.IsNullOrWhiteSpace(file.TargetMod)
                        ? mod.ModId
                        : file.TargetMod;

                if (!validModIds.Contains(targetModId))
                {
                    Console.WriteLine($"[DataManager] Invalid modId '{targetModId}' in '{jsonFile}'.");
                    continue;
                }

                if (!_dataContainers.TryGetValue(targetModId, out var container))
                {
                    container = new DataContainer();
                    _dataContainers[targetModId] = container;
                }

                foreach (var kv in file.Data)
                {
                    var key = kv.Key;
                    var value = kv.Value;

                    string json = JsonSerializer.Serialize(value, _jsonOptions);
                    var element = JsonDocument.Parse(json).RootElement;


                    _registeredCategories.TryGetValue(key, out string? category);
                    if (category != null)
                    {
                        // Category matched: strip the category prefix
                        container.AddToFlatData(element, category, file.SamePathConflict, file.SamePathConflictArray);
                    }
                    else
                    {
                        // No category: keep full path
                        var wrapped = JsonDocument.Parse(
                            JsonSerializer.Serialize(new Dictionary<string, object> { { key, value } }, _jsonOptions)).RootElement;

                        container.AddToFlatData(wrapped, null, file.SamePathConflict, file.SamePathConflictArray);
                    }
                }
            }
        }

        Console.WriteLine(
            $"[DataManager] Loaded data for {_dataContainers.Count} mods.");
    }

    public object? GetData(string modId, string path)
    {
        if (string.IsNullOrWhiteSpace(modId))
            throw new ArgumentException("[DataManager] modId cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("[DataManager] path cannot be null or empty.");

        if (!_dataContainers.TryGetValue(modId, out var container))
            throw new InvalidOperationException($"[DataManager] No data container found for mod '{modId}'.");

        return container.GetFlatData(path);
    }

    public void SetData(string modId, string path, object? value)
    {
        if (string.IsNullOrWhiteSpace(modId))
            throw new ArgumentException("[DataManager] modId cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("[DataManager] path cannot be null or empty.");

        if (!_dataContainers.TryGetValue(modId, out var container))
            throw new InvalidOperationException($"[DataManager] No data container found for mod '{modId}'.");

        if (value == null)
        {
            Console.WriteLine($"[DataManager] Ignored attempt to set null for '{path}' in mod '{modId}'. Value cannot be null.");
            return;
        }

        container.SetFlatData(path, value);
    }

    public void RegisterCategories(IEnumerable<string> categories)
    {
        foreach (var category in categories)
            _registeredCategories.Add(category);
    }

    public DataContainer? TryGetContainer(string modId)
    {
        _dataContainers.TryGetValue(modId, out var container);
        return container;
    }

    public void ClearCategoryIndex(string category)
    {
        foreach (var (_, container) in _dataContainers)
            container.ClearCategoryIndex(category);
    }

    private sealed class JsonDataFile
    {
        // Target mod for this data file.
        // If null or missing, defaults to the current mod.
        public string? TargetMod { get; set; }

        // Conflict resolution (non-array-compatible cases)
        public string? SamePathConflict { get; set; }

        // Conflict resolution (array-compatible cases)
        // string ("ignore" | "overwrite") OR int (insert at index)
        public object? SamePathConflictArray { get; set; }

        // Actual data payload (required)
        public Dictionary<string, object>? Data { get; set; }
    }
}
