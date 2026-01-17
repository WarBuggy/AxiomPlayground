using System.Text.Json;
using AxiomPlayground.GameFlag;
using AxiomPlayground.Modding;
using MoonSharp.Interpreter.Compatibility;

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

    public void LoadAll(List<Mod> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[DataManager] No mods provided.");

        var validModIds = mods.Select(m => m.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var frameworkDebugEnabled = GameFlagManager.IsSet(FrameworkGameFlag.Debug);

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
                    container = new DataContainer(frameworkDebugEnabled);
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
                        container.AddToFlatData(mod.ModId, element, category, file.SamePathConflict, file.SamePathConflictArray);
                    }
                    else
                    {
                        // No category: keep full path
                        var wrapped = JsonDocument.Parse(
                            JsonSerializer.Serialize(new Dictionary<string, object> { { key, value } }, _jsonOptions)).RootElement;

                        container.AddToFlatData(mod.ModId, wrapped, null, file.SamePathConflict, file.SamePathConflictArray);
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

    #region FrameworkGameFlag.Debug

    // Shows the history of a single path for a given mod
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

        var history = container.GetPathHistory(path);

        if (history.Count == 0)
        {
            // GetPathHistory already logs reason (debug disabled or path never existed)
            return;
        }

        Console.WriteLine($"[DataManager] Path history for '{path}' in mod '{modId}':");

        PrintHistoryList(history);
    }

    // Shows all paths and their history for a single mod
    public void ShowAllPathHistories(string modId)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (!_dataContainers.TryGetValue(modId, out var container))
        {
            Console.WriteLine(
                $"[DataManager] Cannot show path histories. No DataContainer found for mod '{modId}'.");
            return;
        }

        var allPaths = container.GetAllFlatData().Keys;

        if (allPaths.Count == 0)
        {
            Console.WriteLine($"[DataManager] No data paths exist for mod '{modId}'.");
            return;
        }

        Console.WriteLine($"[DataManager] Path histories for mod '{modId}':");

        foreach (var path in allPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"- Path: {path}");
            // Reuse ShowPathHistory logic by printing the history directly
            var history = container.GetPathHistory(path);

            if (history.Count == 0)
            {
                Console.WriteLine("  (no history available)");
                continue;
            }

            PrintHistoryList(history);
        }
    }

    // Shows all paths and their histories for all mods
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
            Console.WriteLine($"\n=== Mod: {modId} ===");
            ShowAllPathHistories(modId); // reuse helper for per-mod printing
        }

        Console.WriteLine("\n[DataManager] End of all path histories.");
    }

    // Helper to print a list of path history entries
    private static void PrintHistoryList(IReadOnlyList<(string ModId, string Event, string? CausedBy)> history)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        for (int i = 0; i < history.Count; i++)
        {
            var (ModId, Event, CausedBy) = history[i];

            if (CausedBy == null)
            {
                Console.WriteLine($"  {i + 1}. {Event} by {ModId}");
            }
            else
            {
                Console.WriteLine($"  {i + 1}. {Event} by {ModId} (caused by '{CausedBy}')");
            }
        }
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
