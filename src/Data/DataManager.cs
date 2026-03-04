using System.Data;
using System.Text.Json;
using AxiomPlayground.GameFlag;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Data;

public sealed class DataManager
{
    private static readonly DataManager _instance = new();
    public static DataManager Instance => _instance;
    private const string DATA_FOLDER = "Data";
    private readonly Dictionary<string, DataContainer> _dataContainers = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HashSet<string> _systemCategories = [];
    private DataManager() { }

    public void LoadAll(List<Mod> mods, IReadOnlyList<BaseManager> managers)
    {
        foreach (var manager in managers)
        {
            if (manager.RequiredProcessedPaths)
                _systemCategories.Add(manager.CategoryName);
        }

        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[DataManager] No mods provided.");

        var validModIds = mods.Select(m => m.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var frameworkDebugEnabled = GameFlagManager.IsSet(FrameworkGameFlag.Debug);

        // prepare data container for Core mod
        PrepareCoreDataContainer(mods, frameworkDebugEnabled);

        foreach (var mod in mods)
        {
            // Give every mod a data container
            if (mod.ModId != ModManager.CORE_MOD_ID) // skip Core because already exists
                _dataContainers[mod.ModId] = new DataContainer(frameworkDebugEnabled);

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
                    Console.WriteLine($"[DataManager] Target mod '{targetModId}' must be loaded before being used by mod '{mod.ModId}'. Please check the mod load order!");
                    continue;
                }

                bool isOverwrite = file.SamePathConflict == "overwrite";

                foreach (var kv in file.Data)
                {
                    var key = kv.Key;
                    var value = kv.Value;
                    string? category = null;
                    string keyFirstPart = key.Split(".")[0];
                    if (CategoryExists(keyFirstPart))
                    {
                        category = keyFirstPart;
                    }
                    var flattenedData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    FlattenValue(kv.Value, kv.Key, flattenedData, mod.ModId, jsonFile);
                    container.HandleFlatData(mod.ModId, flattenedData, isOverwrite, category);
                }
            }
        }

        Console.WriteLine($"[DataManager] Loaded data for {_dataContainers.Count} mods.");

        // Process path if required
        foreach (var manager in managers)
        {
            if (!manager.RequiredProcessedPaths)
                continue;

            IReadOnlyList<CategoryData> categoryData = CollectDataFromCategory(manager.CategoryName);
            manager.ProcessPathData(categoryData);
        }
    }

    private static void FlattenValue
    (
        object? value,
        string currentPath,
        Dictionary<string, object?> output,
        string modId,
        string file
    )
    {
        if (value == null)
        {
            output[currentPath] = null;
            return;
        }

        if (value is JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        FlattenValue
                        (
                            prop.Value,
                            $"{currentPath}.{prop.Name}",
                            output, modId, file
                        );
                    }
                    return;

                case JsonValueKind.Array:
                    WarnArrayNotAllowed(modId, file, currentPath);
                    return;

                case JsonValueKind.String:
                    output[currentPath] = element.GetString();
                    return;

                case JsonValueKind.Number:
                    output[currentPath] = element.GetDouble();
                    return;

                case JsonValueKind.True:
                    output[currentPath] = true;
                    return;

                case JsonValueKind.False:
                    output[currentPath] = false;
                    return;

                case JsonValueKind.Null:
                    output[currentPath] = null;
                    return;

                default:
                    WarnInvalidValue(modId, file, currentPath);
                    return;
            }
        }

        if (value is Dictionary<string, object?> dict)
        {
            foreach (var kv in dict)
            {
                FlattenValue
                (
                    kv.Value,
                    $"{currentPath}.{kv.Key}",
                    output, modId, file
                );
            }
            return;
        }

        if (value is IEnumerable<object>)
        {
            WarnArrayNotAllowed(modId, file, currentPath);
            return;
        }

        // Primitive leaf
        output[currentPath] = value;
    }

    private static void WarnArrayNotAllowed(string modId, string file, string path)
    {
        Console.WriteLine($"[DataManager] Warning: arrays are not allowed (mod '{modId}', file '{file}', path '{path}'). Skipping.");
    }

    private static void WarnInvalidValue(string modId, string file, string path)
    {
        Console.WriteLine($"[DataManager] Warning: invalid value (mod '{modId}', file '{file}', path '{path}'). Skipping.");
    }

    public bool TryGetData(string owningModId, string path, out object? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(owningModId))
            throw new ArgumentException("[DataManager] owningModId cannot be null or empty.");

        if (!_dataContainers.TryGetValue(owningModId, out var container))
            throw new InvalidOperationException($"[DataManager] No data container found for mod '{owningModId}'.");

        return container.TryGetFlatData(path, out value);
    }

    public void SetData(string owningModId, string path, string actingModId, object? value)
    {
        if (string.IsNullOrWhiteSpace(owningModId))
            throw new ArgumentException("[DataManager] owningModId cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("[DataManager] path cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(actingModId))
            throw new ArgumentException("[DataManager] actingModId cannot be null or empty.");

        if (!_dataContainers.TryGetValue(owningModId, out var container))
            throw new InvalidOperationException($"[DataManager] No data container found for mod '{owningModId}'.");

        container.SetFlatData(owningModId, path, actingModId, value);
    }

    public IReadOnlyList<CategoryData> CollectDataFromCategory(string category)
    {
        var result = new List<CategoryData>();

        bool debugEnabled = GameFlagManager.IsSet(FrameworkGameFlag.Debug);

        foreach (var kvp in _dataContainers)
        {
            string modId = kvp.Key;
            var container = kvp.Value;

            var paths = container.GetPathsInCategory(category);
            if (paths.Count == 0)
                continue;

            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var history = debugEnabled
                ? new Dictionary<string, PathHistory>(StringComparer.OrdinalIgnoreCase)
                : [];

            foreach (var path in paths)
            {
                if (!container.TryGetFlatData(path, out var value))
                    continue;

                values[path] = value!;

                if (debugEnabled && container.TryGetPathHistory(path, out var pathHistory))
                {
                    history[path] = pathHistory!;
                }
            }

            result.Add(new CategoryData(category, modId, values, history));
        }

        return result;
    }

    // TODO: remove this or make it private
    public DataContainer? TryGetContainer(string modId)
    {
        _dataContainers.TryGetValue(modId, out var container);
        return container;
    }

    private void ClearCategoryIndex(string category)
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
        // Actual data payload (required)
        public Dictionary<string, object>? Data { get; set; }
    }

    private void PrepareCoreDataContainer(List<Mod> mods, bool frameworkDebugEnabled)
    {
        var coreDataContainer = new DataContainer(frameworkDebugEnabled);
        var modLedger = new LedgerArray();
        foreach (var mod in mods)
        {
            modLedger.InsertLast(mod.ModId, ModManager.CORE_MOD_ID);
        }
        if (coreDataContainer.TryCreateFlatData(ModManager.CORE_MOD_ID, "mods.list", ModManager.CORE_MOD_ID, modLedger, out var error))
        {
            _dataContainers[ModManager.CORE_MOD_ID] = coreDataContainer;
            return;
        }
        throw new Exception($"[DataManager] Failed to create path data of all mod list. Error: {error}");
    }

    private bool CategoryExists(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        return _systemCategories.Contains(category);
    }


    #region FrameworkGameFlag.Debug

    public void ShowPathHistory(string modId, string path)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (!_dataContainers.TryGetValue(modId, out var container))
        {
            Console.WriteLine(
                $"[DataManager] Cannot show history for path '{path}'. " +
                $"No DataContainer found for mod '{modId}'.");
            return;
        }

        container.PrintHistoryList(path);
    }

    public void ShowAllPathHistories(string modId)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (!_dataContainers.TryGetValue(modId, out var container))
        {
            Console.WriteLine(
                $"[DataManager] Cannot show path histories. No DataContainer found for mod '{modId}'.");
            return;
        }

        container.PrintAllHistoryList(modId);

    }

    public void ShowAllPathHistoriesForAllMods()
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (_dataContainers.Count == 0)
        {
            Console.WriteLine("[DataManager] No mods have loaded any data paths.");
            return;
        }

        Console.WriteLine("[DataManager] Showing all path histories for all mods:");

        foreach (var modId in _dataContainers.Keys.OrderBy(m => m, StringComparer.OrdinalIgnoreCase))
        {
            ShowAllPathHistories(modId); // reuse helper for per-mod printing
        }

        Console.WriteLine("\n[DataManager] End of all path histories.");
    }

    private static bool CheckAndWarnAboutFrameworkDebug()
    {
        if (GameFlagManager.IsSet(FrameworkGameFlag.Debug))
        {
            return true;
        }

        Console.WriteLine
        (
            "[DataContainer] Framework debug mode is not enabled. " +
            "All functions showing data path history are disabled. " +
            "Start the game with the '-debug' argument to enable full data path history."
        );
        return false;
    }

    #endregion
}
