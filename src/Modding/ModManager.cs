using AxiomPlayground.GameFlag;
using AxiomPlayground.Modding.Discovery;
using AxiomPlayground.Shared;

namespace AxiomPlayground.Modding;

public sealed class ModManager
{
    private static readonly ModManager _instance = new();
    public static ModManager Instance => _instance;
    private readonly List<Mod> _loadedMods = [];
    public IReadOnlyList<Mod> LoadedMods => _loadedMods;
    private readonly Dictionary<string, string> _resolvedModPaths = new(StringComparer.OrdinalIgnoreCase);

    private ModManager() { }

    private static List<ModSelectedState> LoadSelectedModList()
    {
        var selectedMods =
            ModSelectedStore.Load(ModSystemPolicy.SelectedModFilePath);

        return NormalizeSelectedModList(selectedMods);
    }

    private static List<ModSelectedState> NormalizeSelectedModList(
        IEnumerable<ModSelectedState> states)
    {
        var result = states
            .Where(s => !s.ModId.Equals(
                ModSystemPolicy.CORE_MOD_ID,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Core is always first and always Local
        result.Insert(
            0,
            new ModSelectedState(
                ModSystemPolicy.CORE_MOD_ID,
                ModSource.Local,
                0));

        // Rebuild order values after insertion/removal
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Order = i;
        }

        return result;
    }

    public void LoadModsFromSelection()
    {
        var selectedStates = LoadSelectedModList();

        _loadedMods.Clear();
        _resolvedModPaths.Clear();

        foreach (var selected in selectedStates)
        {
            var mod = ModDiscovery.LoadSelectedMod(selected);

            _loadedMods.Add(mod);

            _resolvedModPaths[mod.Info.Id] =
                Path.Combine(
                    ModSourcePathProvider.GetPath(mod.Source),
                    mod.Info.Id);
        }
    }

    /// <summary>
    /// Compute the full folder path for a given mod.
    /// </summary>
    public string GetModFolderPath(Mod mod)
    {
        ArgumentNullException.ThrowIfNull(mod);

        if (_resolvedModPaths.TryGetValue(mod.Info.Id, out var path))
            return path;

        throw new ArgumentException(
            $"[ModManager] Mod '{mod.Info.Id}' is not loaded.");
    }

    public string GetModFolderPath(string modId)
    {
        if (_resolvedModPaths.TryGetValue(modId, out var path))
            return path;

        throw new ArgumentException($"[ModManager] Unknown modId: {modId}");
    }

    public bool TryGetMod(string modId, out Mod mod)
    {
        mod = null!;

        if (string.IsNullOrWhiteSpace(modId))
            return false;

        var found = _loadedMods.Find(
            m => m.Info.Id.Equals(modId, StringComparison.OrdinalIgnoreCase));

        if (found == null)
            return false;

        mod = found;
        return true;
    }

    #region FrameworkGameFlag.Debug

    public void ShowRuntimeHistory(string modId, string key)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (!TryGetMod(modId, out var mod))
        {
            Console.WriteLine(
                $"[ModManager] Cannot show runtime history for key '{key}'. " +
                $"No mod found with id '{modId}'.");
            return;
        }

        var history = mod.GetRuntimeHistory(key);

        if (history.Count == 0)
        {
            // GetRuntimeHistory already logs reason
            return;
        }

        Console.WriteLine($"[ModManager] Runtime history for key '{key}' in mod '{modId}':");

        PrintRuntimeHistoryList(history);
    }

    public void ShowAllRuntimeHistories(string modId)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (!TryGetMod(modId, out var mod))
        {
            Console.WriteLine(
                $"[ModManager] Cannot show runtime histories. No mod found with id '{modId}'.");
            return;
        }

        var allKeys = mod.GetAllRuntimeHistoryKeys();

        if (allKeys.Count == 0)
        {
            Console.WriteLine($"[ModManager] No runtime keys exist for mod '{modId}'.");
            return;
        }

        Console.WriteLine($"[ModManager] Runtime histories for mod '{modId}':");

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"- Key: {key}");

            var history = mod.GetRuntimeHistory(key);
            if (history.Count == 0)
            {
                Console.WriteLine("  (no history available)");
                continue;
            }

            PrintRuntimeHistoryList(history);
        }
    }

    public void ShowAllRuntimeHistoriesForAllMods()
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        if (LoadedMods.Count == 0)
        {
            Console.WriteLine("[ModManager] No mods are loaded.");
            return;
        }

        Console.WriteLine("[ModManager] Showing all runtime histories for all mods:");

        foreach (var mod in LoadedMods.OrderBy(m => m.Info.Id, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"\n=== Mod: {mod.Info.Id} ===");
            ShowAllRuntimeHistories(mod.Info.Id);
        }

        Console.WriteLine("\n[ModManager] End of all runtime histories.");
    }

    private static void PrintRuntimeHistoryList(
        IReadOnlyList<(string ModId, string Event, object? Value)> history)
    {
        if (!CheckAndWarnAboutFrameworkDebug()) return;

        for (int i = 0; i < history.Count; i++)
        {
            var (modId, evt, value) = history[i];

            string? valueInString = value != null ? value.ToString() : "<null>";
            Console.WriteLine($"  {i + 1}. {evt} by {modId} (value = {valueInString})");
        }
    }

    private static bool CheckAndWarnAboutFrameworkDebug()
    {
        if (GameFlagManager.IsSet(FrameworkGameFlag.Debug))
            return true;

        Console.WriteLine
        (
            "[ModManager] Framework debug mode is not enabled. " +
            "All runtime history debug functions are disabled. " +
            "Start the game with the '-debug' argument to enable runtime history tracking."
        );
        return false;
    }

    #endregion

}
