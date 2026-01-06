using System.Text.Json;
using MoonSharp.Interpreter;
using AxiomPlayground.Modding;
using AxiomPlayground.Scripting.LuaBindings;

namespace AxiomPlayground.Scripting;

public sealed class ScriptManager
{
    private static readonly ScriptManager _instance = new();
    public static ScriptManager Instance => _instance;
    private const string SCRIPT_FOLDER = "Scripts";

    // modId -> relative script path -> ScriptEntry
    private readonly Dictionary<string, Dictionary<string, ScriptEntry>> _scripts = new(StringComparer.OrdinalIgnoreCase);

    // For logging ignored scripts due to invalid overwrite
    private readonly HashSet<string> _ignoredScripts = [];
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, };

    // Tracks the modId of the script currently being executed
    private string? _currentExecutingModId;
    public string CurrentExecutingModId => _currentExecutingModId ?? "Unknown";

    private ScriptManager() { }

    /// <summary>
    /// Loads all Lua scripts from the given mods.
    /// </summary>
    public void LoadAll(List<ModInstance> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[ScriptManager] No mods provided.");

        var validModIds = mods.Select(m => m.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods)
        {
            string scriptsRoot = Path.Combine(ModManager.Instance.GetModFolderPath(mod), SCRIPT_FOLDER);

            if (!Directory.Exists(scriptsRoot))
                continue;

            int newCount = 0;        // new script for current mod
            int addCount = 0;        // successfully added to another mod
            int overwriteCount = 0;  // overwrites (any mod)
            HashSet<string> affectedMods = []; // includes current mod

            foreach (var luaFile in Directory.GetFiles(scriptsRoot, "*.lua", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(scriptsRoot, luaFile).Replace("\\", "/");
                string metaPath = Path.ChangeExtension(luaFile, ".json");
                ScriptMeta? meta = null;

                if (File.Exists(metaPath))
                {
                    try
                    {
                        meta = JsonSerializer.Deserialize<ScriptMeta>(File.ReadAllText(metaPath), _jsonSerializerOptions);
                    }
                    catch
                    {
                        meta = null;
                    }
                }

                bool hasOverwrite = !string.IsNullOrEmpty(meta?.Overwrite);
                bool hasAddTo = !string.IsNullOrEmpty(meta?.AddTo);

                // Explicit precedence log
                if (hasOverwrite && hasAddTo)
                {
                    Console.WriteLine($"[ScriptManager] Script '{relativePath}' in mod '{mod.ModId}' specifies both overwrite and addTo. Using overwrite.");
                }

                string targetModId = mod.ModId;
                bool isOverwrite = false;
                bool isAddTo = false;

                if (hasOverwrite)
                {
                    if (!validModIds.Contains(meta!.Overwrite!))
                    {
                        Console.WriteLine($"[ScriptManager] Script '{relativePath}' in mod '{mod.ModId}' specifies invalid overwrite target '{meta.Overwrite}'.");
                        continue;
                    }

                    targetModId = meta.Overwrite!;
                    isOverwrite = true;
                }
                else if (hasAddTo)
                {
                    if (!validModIds.Contains(meta!.AddTo!))
                    {
                        Console.WriteLine($"[ScriptManager] Script '{relativePath}' in mod '{mod.ModId}' specifies invalid addTo target '{meta.AddTo}'.");
                        continue;
                    }

                    targetModId = meta.AddTo!;
                    isAddTo = true;
                }

                if (!_scripts.TryGetValue(targetModId, out var modDict))
                {
                    modDict = new Dictionary<string, ScriptEntry>(StringComparer.OrdinalIgnoreCase);
                    _scripts[targetModId] = modDict;
                }

                bool pathExists = modDict.ContainsKey(relativePath);

                // Enforce path rules
                if (isOverwrite && !pathExists)
                {
                    Console.WriteLine($"[ScriptManager] Overwrite ignored: script '{relativePath}' in mod '{mod.ModId}' targets '{targetModId}' but no script exists at that path.");
                    continue;
                }

                if (isAddTo && pathExists)
                {
                    Console.WriteLine($"[ScriptManager] AddTo ignored: script '{relativePath}' in mod '{mod.ModId}' targets '{targetModId}' but path already exists.");
                    continue;
                }

                modDict[relativePath] = new ScriptEntry(luaFile, targetModId, relativePath, mod.ModId, isOverwrite);
                affectedMods.Add(targetModId);

                if (isOverwrite)
                {
                    overwriteCount++;
                }
                else if (isAddTo && !string.Equals(targetModId, mod.ModId, StringComparison.OrdinalIgnoreCase))
                {
                    addCount++;
                }
                else
                {
                    newCount++;
                }
            }

            Console.WriteLine($"[ScriptManager] Mod '{mod.ModId}': new = {newCount}, addTo = {addCount}, overwrite = {overwriteCount}, affectedMods = [{string.Join(", ", affectedMods)}].");
        }

        Console.WriteLine($"[ScriptManager] Loaded {_scripts.Sum(m => m.Value.Count)} Lua scripts across {mods.Count} mods.");
    }

    public void RunAllLoadedScripts()
    {
        foreach (var (modId, modScripts) in _scripts)
        {
            foreach (var scriptEntry in modScripts.Values)
            {
                var luaScript = new Script(CoreModules.Preset_Default);
                LuaBindingRegistrar.RegisterAllBindings(luaScript);
                scriptEntry.Execute(luaScript);
            }
        }
    }


    /// <summary>
    /// Gets a loaded ScriptEntry by modId and relative path.
    /// Returns null if not found.
    /// </summary>
    public ScriptEntry? GetScript(string modId, string relativePath)
    {
        if (_scripts.TryGetValue(modId, out var modDict) &&
            modDict.TryGetValue(relativePath.Replace("\\", "/"), out var entry))
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// Represents a loaded Lua script.
    /// </summary>
    public sealed class ScriptEntry(string filePath, string modId, string relativePath, string owningModId, bool isOverwrite)
    {
        public string FilePath { get; } = filePath;
        public string ModId { get; } = modId;
        public string RelativePath { get; } = relativePath;
        public string OwningModId { get; } = owningModId;
        public bool IsOverwrite { get; } = isOverwrite;

        /// <summary>
        /// Executes the script using MoonSharp.
        /// </summary>
        public void Execute(Script luaScript)
        {
            var previousModId = Instance._currentExecutingModId; // save old value in case of nested calls
            Instance._currentExecutingModId = ModId; // set current executing mod

            try
            {
                string code = File.ReadAllText(FilePath);
                luaScript.DoString(code, codeFriendlyName: RelativePath);
            }
            catch (ScriptRuntimeException ex)
            {
                Console.WriteLine(
                    $"[ScriptManager][LUA ERROR]\n" +
                    $"  Executing Mod      : {ModId}\n" +
                    $"  Script Path        : {RelativePath}\n" +
                    $"  Owning Mod         : {OwningModId}\n" +
                    $"  Overwrite          : {IsOverwrite}\n" +
                    $"{ex.DecoratedMessage ?? ex.Message}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ScriptManager][ERROR]\n" +
                    $"  Executing Mod      : {ModId}\n" +
                    $"  Script Path        : {RelativePath}\n" +
                    $"  Owning Mod         : {OwningModId}\n" +
                    $"  Overwrite          : {IsOverwrite}\n" +
                    $"{ex}"
                );
            }
            finally
            {
                Instance._currentExecutingModId = previousModId;
            }
        }
    }

    /// <summary>
    /// Represents the optional meta file for a Lua script.
    /// </summary>
    private sealed class ScriptMeta
    {
        public string? Overwrite { get; set; }
        public string? AddTo { get; set; }
    }
}
