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

    // modId -> Lua Script instance (one per mod)
    private readonly Dictionary<string, Script> _modLuaScripts = new(StringComparer.OrdinalIgnoreCase);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    // Tracks the mod currently executing a script
    private string? _currentExecutingModId;
    public string CurrentExecutingModId => _currentExecutingModId ?? "Unknown";
    private readonly List<Type> _luaBindingTypes;
    private readonly LuaEventBus _eventBus = new();

    private ScriptManager()
    {
        _luaBindingTypes = [.. AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.IsSubclassOf(typeof(LuaBindingBase)))];
    }

    /// <summary>
    /// Load all Lua scripts from the given mods.
    /// Scripts are stored but not executed yet.
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
                    catch
                    {
                        meta = null;
                    }
                }

                bool hasOverwrite = !string.IsNullOrEmpty(meta?.Overwrite);
                bool hasAddTo = !string.IsNullOrEmpty(meta?.AddTo);

                // Overwrite takes precedence
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

                bool pathExists = _scripts.TryGetValue(targetModId, out var targetModDict) && targetModDict.ContainsKey(relativePath);

                // Enforce rules
                if (isOverwrite && !pathExists)
                {
                    Console.WriteLine($"[ScriptManager] Overwrite ignored: '{relativePath}' targets '{targetModId}' but no script exists at that path.");
                    continue;
                }
                if (isAddTo && pathExists)
                {
                    Console.WriteLine($"[ScriptManager] AddTo ignored: '{relativePath}' targets '{targetModId}' but path already exists.");
                    continue;
                }

                if (!_scripts.TryGetValue(targetModId, out targetModDict))
                {
                    targetModDict = new Dictionary<string, ScriptEntry>(StringComparer.OrdinalIgnoreCase);
                    _scripts[targetModId] = targetModDict;
                }

                targetModDict[relativePath] = new ScriptEntry(luaFile, targetModId, relativePath, mod.ModId, isOverwrite);
            }
        }
    }

    /// <summary>
    /// Executes all scripts for all mods.
    /// Top-level code in scripts runs once per mod.
    /// Event functions (e.g., OnDraw.Add) are available and preserved.
    /// </summary>
    public void ExecuteAllModsScripts()
    {
        int expectedBindingCount = _luaBindingTypes.Count;

        foreach (var modId in _scripts.Keys)
        {
            if (!_modLuaScripts.TryGetValue(modId, out var luaScript))
            {
                luaScript = new Script(CoreModules.Preset_Default);

                var registeredBindings =
                    LuaBindingRegistrar.RegisterAllBindings(luaScript, _luaBindingTypes);
                int registeredCount = registeredBindings.Count;
                if (registeredCount == expectedBindingCount)
                {
                    Console.WriteLine(
                        $"[ScriptManager] Lua bindings registered for mod '{modId}': {registeredCount}/{expectedBindingCount}");
                }
                else
                {
                    Console.WriteLine(
                        $"[ScriptManager][WARNING] Lua bindings mismatch for mod '{modId}': " +
                        $"{registeredCount}/{expectedBindingCount}");

                    var missing = _luaBindingTypes
                        .Select(t => t.FullName ?? t.Name)
                        .Except(registeredBindings);

                    foreach (var m in missing)
                        Console.WriteLine($"  - Missing binding: {m}");
                }

                var eventBinding = new EventLuaBinding(_eventBus, modId);
                eventBinding.Register(luaScript);

                _modLuaScripts[modId] = luaScript;
            }

            foreach (var scriptEntry in _scripts[modId].Values)
            {
                scriptEntry.Execute(luaScript);
            }
        }
    }

    /// <summary>
    /// Returns the Lua Script instance for a mod.
    /// Use this when running individual scripts or events.
    /// </summary>
    public Script GetModScript(string modId)
    {
        if (!_modLuaScripts.TryGetValue(modId, out var luaScript))
            throw new KeyNotFoundException($"No Lua script instance found for mod '{modId}'. Did you forget to execute ExecuteAllModsScripts()?");
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
            string handlerModId = kv.Key;
            var previousModId = Instance._currentExecutingModId;
            Instance._currentExecutingModId = handlerModId;

            foreach (var fn in kv.Value)
            {
                try
                {
                    fn.Call(args);
                }
                catch (ScriptRuntimeException ex)
                {
                    Console.WriteLine($"[ScriptManager] Event '{eventName}' (mod '{handlerModId}') encountered error: {ex.DecoratedMessage}");
                }
            }
            Instance._currentExecutingModId = previousModId;
        }
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
        /// Executes the script using a provided Lua Script instance.
        /// </summary>
        public void Execute(Script luaScript)
        {
            var previousModId = Instance._currentExecutingModId;
            Instance._currentExecutingModId = ModId;

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
    /// Optional meta file for a Lua script.
    /// </summary>
    private sealed class ScriptMeta
    {
        public string? Overwrite { get; set; }
        public string? AddTo { get; set; }
    }
}
