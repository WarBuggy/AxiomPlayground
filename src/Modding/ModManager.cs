namespace AxiomPlayground.Modding;

public enum ModSource
{
    Steam,
    Local
}

public sealed class ModInstance(string modId, ModSource source)
{
    public string ModId { get; set; } = modId;
    public string DisplayName { get; set; } = modId;
    public ModSource Source { get; set; } = source;
    public bool Enable { get; set; } = true;
}

public sealed class ModManager
{
    private static readonly ModManager _instance = new();
    public static ModManager Instance => _instance;
    private const string CORE_MOD_ID = "Core";
    // Order matters: earlier mods load first and can be overridden by later ones
    private readonly List<string> RESERVED_MOD_IDS =
        [
            CORE_MOD_ID,
            "DLC1",
            "DLC2",
            "DLC3",
        ];
    // Single dictionary: ModSource -> (BasePath, Mods dictionary)
    private readonly Dictionary<ModSource, (string Path, Dictionary<string, ModInstance> Mods)> _mods
        = new()
    {
        { ModSource.Steam, ("Steam/workshop/content/gameId/", new Dictionary<string, ModInstance>(StringComparer.OrdinalIgnoreCase)) },
        { ModSource.Local, ("Mods/", new Dictionary<string, ModInstance>(StringComparer.OrdinalIgnoreCase)) }
    };

    private ModManager() { }

    /// <summary>
    /// Scan both Steam and Local folders, reconcile duplicates, and print discovered mods.
    /// </summary>
    public void DiscoverMods()
    {
        ScanFolder(ModSource.Steam);
        ScanFolder(ModSource.Local);
        EnforceReservedModConstraints();
        ReconcileDuplicates();
    }

    /// <summary>
    /// Scan a single folder for mods of the given source.
    /// </summary>
    private void ScanFolder(ModSource source)
    {
        if (!_mods.TryGetValue(source, out var tuple))
            throw new ArgumentException($"[AxiomPlayground.ModManager] Unknown mod source: {source}.");

        string folderPath = tuple.Path;
        var targetDict = tuple.Mods;

        if (!Directory.Exists(folderPath))
            throw new ArgumentException($"[AxiomPlayground.ModManager] Mod folder not found: {folderPath}.");


        foreach (var modDir in Directory.GetDirectories(folderPath))
        {
            string modId = Path.GetFileName(modDir);

            // Remove any existing entry with the same modId before adding the new one
            targetDict.Remove(modId);
            targetDict[modId] = new ModInstance(modId, source);
        }
    }

    /// <summary>
    /// Reconcile mods that exist in both Steam and Local folders by modifying DisplayName.
    /// </summary>
    private void ReconcileDuplicates()
    {
        var steamMods = _mods[ModSource.Steam].Mods;
        var localMods = _mods[ModSource.Local].Mods;

        foreach (var modId in steamMods.Keys)
        {
            if (localMods.TryGetValue(modId, out ModInstance? localMod))
            {
                steamMods[modId].DisplayName = $"{modId} (Steam)";
                localMod.DisplayName = $"{modId} (Local)";
            }
        }
    }

    /// <summary>
    /// Compute the full folder path for a given mod.
    /// </summary>
    public string GetModFolderPath(ModInstance mod)
    {
        if (!_mods.TryGetValue(mod.Source, out var tuple))
            throw new ArgumentException($"[AxiomPlayground.ModManager] Unknown mod source: {mod.Source}.");

        return Path.Combine(tuple.Path, mod.ModId);
    }

    private void EnforceReservedModConstraints()
    {
        var localMods = _mods[ModSource.Local].Mods;
        // Core must exist in Local
        if (!localMods.ContainsKey(CORE_MOD_ID))
            throw new InvalidOperationException($"[AxiomPlayground.ModManager] Mod '{CORE_MOD_ID}' must exist in Local mods.");

        foreach (var modId in RESERVED_MOD_IDS)
        {
            // Reserved mod Id cannot be in any other source
            foreach (var (source, tuple) in _mods)
            {
                if (source == ModSource.Local)
                    continue;

                tuple.Mods.Remove(modId);
            }
        }
    }

    public List<ModInstance> GetAllModsSortedByDisplayName()
    {
        var result = new List<ModInstance>();

        var allMods = new List<ModInstance>();
        foreach (var tuple in _mods.Values)
            allMods.AddRange(tuple.Mods.Values);

        foreach (var modId in RESERVED_MOD_IDS)
        {
            var mod = allMods.Find(m => m.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase));
            if (mod != null)
            {
                result.Add(mod);
                allMods.Remove(mod);
            }
        }

        allMods.Sort((a, b) =>
        string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

        result.AddRange(allMods);
        return result;
    }


    /// <summary>
    /// Given a list of mods, removes duplicates based on ModId, keeping only the last occurrence.
    /// </summary>
    public static List<ModInstance> GetFinalLoadableMods(List<ModInstance> mods)
    {
        var finalMods = new Dictionary<string, ModInstance>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            // Each time we encounter a ModId, overwrite previous entry
            finalMods[mod.ModId] = mod;
        }
        return [.. finalMods.Values];
    }

}
