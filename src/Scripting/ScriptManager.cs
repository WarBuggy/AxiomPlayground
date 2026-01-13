using System.Text.Json;
using MoonSharp.Interpreter;
using AxiomPlayground.Modding;
using AxiomPlayground.Scripting.LuaBindings;
using AxiomPlayground.Scripting.Libraries;

namespace AxiomPlayground.Scripting;

public sealed class ScriptManager
{
    private static readonly ScriptManager _instance = new();
    public static ScriptManager Instance => _instance;

    private const string SCRIPT_FOLDER = "Scripts";
    public static readonly string CURRENT_SCRIPT_PATH_KEY = "_CurrentScriptPath";

    // modId -> relative script path -> ScriptEntry
    private readonly Dictionary<string, Dictionary<string, ScriptEntry>> _scripts =
        new(StringComparer.OrdinalIgnoreCase);

    // modId -> Lua Script instance (one per mod)
    private readonly Dictionary<string, Script> _modLuaScripts = new(StringComparer.OrdinalIgnoreCase);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private string? _currentExecutingModId;
    public string CurrentExecutingModId => _currentExecutingModId ?? "Unknown";

    private readonly List<Type> _luaBindingTypes;
    private readonly LuaEventBus _eventBus = new();

    private ScriptManager()
    {
        _luaBindingTypes =
        [
            .. AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return []; }
                })
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsSubclassOf(typeof(LuaBindingBase)))
        ];
    }

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

            if (!_scripts.TryGetValue(mod.ModId, out var modDict))
            {
                modDict = new Dictionary<string, ScriptEntry>(StringComparer.OrdinalIgnoreCase);
                _scripts[mod.ModId] = modDict;
            }

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
                    catch { meta = null; }
                }

                bool hasOverwrite = !string.IsNullOrEmpty(meta?.Overwrite);
                bool hasAddTo = !string.IsNullOrEmpty(meta?.AddTo);
                string? libraryName = meta?.Library;
                bool hasLibrary = !string.IsNullOrEmpty(libraryName);
                string? patchLibraryName = meta?.PatchLibrary;
                bool hasLibraryPatch = !string.IsNullOrEmpty(patchLibraryName);

                string targetModId = mod.ModId;
                string? targetLibraryName = null;
                bool isOverwrite = false;
                bool isLibrary = false;

                if (hasOverwrite)
                {
                    targetModId = meta!.Overwrite!;
                    if (!validModIds.Contains(targetModId))
                    {
                        Console.WriteLine($"[ScriptManager] Overwrite target '{mod.ModId}' -> '{targetModId}' invalid. Skipping '{relativePath}'.");
                        continue;
                    }
                    isOverwrite = true;
                }
                else if (hasAddTo)
                {
                    targetModId = meta!.AddTo!;
                    if (!validModIds.Contains(targetModId))
                    {
                        Console.WriteLine($"[ScriptManager] AddTo target '{mod.ModId}' -> '{targetModId}' invalid. Skipping '{relativePath}'.");
                        continue;
                    }
                }
                else if (hasLibraryPatch)
                {
                    targetLibraryName = patchLibraryName;
                }
                else if (hasLibrary)
                {
                    isLibrary = true;
                    targetLibraryName = libraryName;
                }

                if (targetLibraryName != null)
                {
                    HandleLibraryOrPatchScript(mod.ModId, isLibrary, targetLibraryName!, relativePath, luaFile);
                    continue;
                }

                _scripts.TryGetValue(targetModId, out var modScripts);
                bool targetScriptExists = modScripts != null && modScripts!.ContainsKey(relativePath);
                if (isOverwrite && !targetScriptExists)
                {
                    Console.WriteLine($"[ScriptManager] Overwrite ignored: Mod '{mod.ModId}', '{relativePath}' targets '{targetModId}' but no script exists.");
                    continue;
                }
                else if (!isOverwrite && targetScriptExists)
                {
                    Console.WriteLine($"[ScriptManager] AddTo ignored: Mod '{mod.ModId}', '{relativePath}' targets '{targetModId}' but path already exists.");
                    continue;
                }

                // Only non-library and non-library-patch script reaches here
                if (modScripts == null)
                {
                    modScripts = new Dictionary<string, ScriptEntry>(StringComparer.OrdinalIgnoreCase);
                    _scripts[targetModId] = modScripts;
                }
                modScripts[relativePath] = new ScriptEntry(luaFile, targetModId, relativePath, mod.ModId, isOverwrite);
            }
        }
    }

    private static void HandleLibraryOrPatchScript(string modId, bool isLibrary, string targetLibraryName, string relativePath, string luaFile)
    {
        bool targetLibraryExists = LibraryManager.Instance.CheckNameExists(targetLibraryName);
        if (isLibrary)
        {
            if (targetLibraryExists)
            {
                Console.WriteLine(
                        $"[ScriptManager] Library name '{targetLibraryName}' already declared by another script. Skipping '{relativePath}'.");
                return;
            }
            string libraryLuaText = File.ReadAllText(luaFile);
            LibraryManager.Instance.RegisterLibrary(modId, targetLibraryName!, null, relativePath, libraryLuaText);
            return;
        }


        if (!targetLibraryExists)
        {
            Console.WriteLine(
                   $"[ScriptManager] Library name '{targetLibraryName}' does not exist. Skipping '{relativePath}'.");
            return;
        }
        string patchLuaText = File.ReadAllText(luaFile);
        LibraryManager.Instance.AddLibraryPatch(modId, targetLibraryName!, relativePath, patchLuaText);
    }

    private (HashSet<string> failedLibraries, Dictionary<string, HashSet<string>> failedPatches) TestAllLibraries()
    {
        var failedLibraries = new HashSet<string>();
        var failedPatches = new Dictionary<string, HashSet<string>>();

        // Temporary VM for testing
        var testVm = new Script(CoreModules.Preset_Default);
        LuaBindingRegistrar.RegisterAllBindings(testVm, _luaBindingTypes);
        new EventLuaBinding(_eventBus, "dummyVM").Register(testVm);

        foreach (var library in LibraryManager.Instance.GetAllLibraries().OrderBy(l => l.PublishOrder))
        {
            try
            {
                testVm.DoString(library.LuaText, codeFriendlyName: library.PublishScriptPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptManager] Test run: Error in library '{library.LibraryName}': {ex}. This library will be skipped.");
                failedLibraries.Add(library.LibraryName);
                continue; // Skip its patches
            }

            // Test patches
            foreach (var patchList in library.PatchScriptsByMod.Values)
            {
                foreach (var (relativePath, luaText) in patchList)
                {
                    if (!failedPatches.ContainsKey(library.LibraryName))
                        failedPatches[library.LibraryName] = [];

                    try
                    {
                        testVm.DoString(luaText, codeFriendlyName: relativePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ScriptManager] Test run: Error in patch '{relativePath}' of library '{library.LibraryName}': {ex}. This patch will be skipped.");
                        failedPatches[library.LibraryName].Add(relativePath);
                    }
                }
            }
        }

        return (failedLibraries, failedPatches);
    }

    public void ExecuteAllModsScripts()
    {
        var (failedLibraries, failedPatches) = TestAllLibraries();

        foreach (var modId in _scripts.Keys)
        {
            if (!_modLuaScripts.TryGetValue(modId, out var luaScript))
            {
                luaScript = new Script(CoreModules.Preset_Default);
                LuaBindingRegistrar.RegisterAllBindings(luaScript, _luaBindingTypes);
                new EventLuaBinding(_eventBus, modId).Register(luaScript);
                _modLuaScripts[modId] = luaScript;
            }

            // Execute all libraries and patches first
            foreach (var library in LibraryManager.Instance.GetAllLibraries().OrderBy(l => l.PublishOrder))
            {
                if (failedLibraries.Contains(library.LibraryName))
                    continue; // Skip library that failed previously

                try
                {
                    luaScript.DoString(library.LuaText, codeFriendlyName: library.PublishScriptPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScriptManager] Unexpected error injecting library '{library.LibraryName}' to mod '{modId}': {ex}.");
                }

                // Execute patches
                foreach (var patchList in library.PatchScriptsByMod.Values)
                {
                    foreach (var (relativePath, luaText) in patchList)
                    {
                        if (failedPatches.TryGetValue(library.LibraryName, out var failedPatchSet) &&
                            failedPatchSet.Contains(relativePath))
                            continue; // Skip previously failed patch

                        try
                        {
                            luaScript.DoString(luaText, codeFriendlyName: relativePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ScriptManager] Unexpected error injecting patch '{relativePath}' of library '{library.LibraryName}' to mod '{modId}': {ex}.");
                        }
                    }
                }
            }

            // Execute normal scripts for this mod
            foreach (var scriptEntry in _scripts[modId].Values)
            {
                scriptEntry.Execute(luaScript);
            }
        }
    }

    public Script GetModScript(string modId)
    {
        if (!_modLuaScripts.TryGetValue(modId, out var luaScript))
            throw new KeyNotFoundException(
                $"[ScriptManager] No Lua script instance found for mod '{modId}'.");

        return luaScript;
    }

    public void Fire(string eventName, params DynValue[] args)
    {
        // LuaEventBus is still used to store handlers
        _eventBus.TryGetHandlers(eventName, out var handlers);
        if (handlers == null)
            return;

        foreach (var kv in handlers)
        {
            var previous = _currentExecutingModId;
            _currentExecutingModId = kv.Key;

            foreach (var fn in kv.Value)
            {
                try { fn.Call(args); }
                catch (ScriptRuntimeException) { }
            }

            _currentExecutingModId = previous;
        }
    }

    /// <summary>
    /// Represents a single Lua script discovered by ScriptManager.
    /// A script may be a normal script or a library script.
    /// Library scripts may have patch scripts attached to them.
    /// </summary>
    public sealed class ScriptEntry(
        string filePath,
        string modId,
        string relativePath,
        string owningModId,
        bool isOverwrite)
    {
        public string FilePath { get; } = filePath ?? throw new ArgumentNullException(nameof(filePath));
        public string ModId { get; } = modId ?? throw new ArgumentNullException(nameof(modId));
        public string RelativePath { get; } = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        public string OwningModId { get; } = owningModId ?? throw new ArgumentNullException(nameof(owningModId));
        public bool IsOverwrite { get; } = isOverwrite;

        public void Execute(Script luaScript)
        {
            var previousModId = ScriptManager.Instance._currentExecutingModId;
            ScriptManager.Instance._currentExecutingModId = ModId;

            try
            {
                string code = File.ReadAllText(FilePath);
                luaScript.Globals[ScriptManager.CURRENT_SCRIPT_PATH_KEY] = RelativePath;
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
                    $"[ScriptManager][LUA ERROR]\n" +
                    $"  Executing Mod      : {ModId}\n" +
                    $"  Script Path        : {RelativePath}\n" +
                    $"  Owning Mod         : {OwningModId}\n" +
                    $"  Overwrite          : {IsOverwrite}\n" +
                    $"{ex}"
                );
            }
            finally
            {
                ScriptManager.Instance._currentExecutingModId = previousModId;
            }
        }
    }

    private sealed class ScriptMeta
    {
        public string? Overwrite { get; set; }
        public string? AddTo { get; set; }
        public string? Library { get; set; }
        public string? PatchLibrary { get; set; }
    }
}
