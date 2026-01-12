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
    private readonly Dictionary<string, Script> _modLuaScripts =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

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

                string targetModId = mod.ModId;
                bool isOverwrite = false;
                bool isLibrary = false;

                if (hasOverwrite)
                {
                    targetModId = meta!.Overwrite!;
                    if (!validModIds.Contains(targetModId))
                    {
                        Console.WriteLine($"[ScriptManager] Overwrite target '{targetModId}' invalid. Skipping '{relativePath}'.");
                        continue;
                    }
                    isOverwrite = true;
                }
                else if (hasAddTo)
                {
                    targetModId = meta!.AddTo!;
                    if (!validModIds.Contains(targetModId))
                    {
                        Console.WriteLine($"[ScriptManager] AddTo target '{targetModId}' invalid. Skipping '{relativePath}'.");
                        continue;
                    }
                }
                else if (hasLibrary)
                {
                    bool nameExists = _scripts.Values
                        .SelectMany(modDict => modDict.Values)
                        .Any(entry => entry.IsLibrary && entry.LibraryName != null &&
                                    entry.LibraryName.Equals(libraryName, StringComparison.OrdinalIgnoreCase));


                    if (nameExists)
                    {
                        Console.WriteLine(
                            $"[ScriptManager] Library name '{libraryName}' already declared by another script. Skipping '{relativePath}'.");
                        continue;
                    }
                    isLibrary = true;
                }

                bool targetExists = _scripts.TryGetValue(targetModId, out var targetDict) && targetDict.ContainsKey(relativePath);

                if (isOverwrite && !targetExists)
                {
                    Console.WriteLine($"[ScriptManager] Overwrite ignored: '{relativePath}' targets '{targetModId}' but no script exists.");
                    continue;
                }
                if (hasAddTo && targetExists)
                {
                    Console.WriteLine($"[ScriptManager] AddTo ignored: '{relativePath}' targets '{targetModId}' but path already exists.");
                    continue;
                }

                if (!_scripts.TryGetValue(targetModId, out targetDict))
                {
                    targetDict = new Dictionary<string, ScriptEntry>(StringComparer.OrdinalIgnoreCase);
                    _scripts[targetModId] = targetDict;
                }

                targetDict[relativePath] = new ScriptEntry(
                    luaFile,
                    targetModId,
                    relativePath,
                    mod.ModId,
                    isOverwrite,
                    isLibrary,
                    libraryName
                );
            }
        }
    }

    public void ExecuteAllModsScripts()
    {
        int expectedBindingCount = _luaBindingTypes.Count;

        foreach (var modId in _scripts.Keys)
        {
            if (!_modLuaScripts.TryGetValue(modId, out var luaScript))
            {
                luaScript = new Script(CoreModules.Preset_Default);

                LuaBindingRegistrar.RegisterAllBindings(luaScript, _luaBindingTypes);

                new EventLuaBinding(_eventBus, modId).Register(luaScript);

                new LibraryLuaBinding(modId).Register(luaScript);

                _modLuaScripts[modId] = luaScript;
            }

            foreach (var scriptEntry in
                     _scripts[modId].Values.OrderByDescending(s => s.IsLibrary))
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
    /// Represents a loaded Lua script.
    /// </summary>
    public sealed class ScriptEntry(string filePath, string modId, string relativePath, string owningModId, bool isOverwrite, bool isLibrary, string? libraryName = null)
    {
        public string FilePath { get; } = filePath;
        public string ModId { get; } = modId;
        public string RelativePath { get; } = relativePath;
        public string OwningModId { get; } = owningModId;
        public bool IsOverwrite { get; } = isOverwrite;
        public bool IsLibrary { get; } = isLibrary;
        public string? LibraryName { get; } = libraryName;

        /// <summary>
        /// Executes the script using a provided Lua Script instance.
        /// If IsLibrary is true, publishes the library to LibraryManager.
        /// </summary>
        public void Execute(Script luaScript)
        {
            var previousModId = ScriptManager.Instance._currentExecutingModId;
            ScriptManager.Instance._currentExecutingModId = ModId;

            try
            {
                string code = File.ReadAllText(FilePath);
                luaScript.Globals[ScriptManager.CURRENT_SCRIPT_PATH_KEY] = RelativePath;

                if (IsLibrary && LibraryName != null)
                {
                    LibraryManager.Instance.RegisterLibrary(
                        publishingModId: ModId,
                        libraryName: LibraryName,
                        libraryTable: null,        // Will be created when fetched
                        publishScriptPath: RelativePath,
                        luaText: code              // Store the Lua text for later execution
                    );

                    Console.WriteLine(
                        $"[ScriptManager] Library registered: {ModId}.{LibraryName} from '{RelativePath}'.");

                    // Inject as global in the mod that owns it
                    // To do after we get Get and Patch working
                    // Lazy placeholder in the mod that owns the library
                    luaScript.Globals[LibraryName] = new Table(luaScript);
                }
                else
                {
                    // Normal script execution
                    luaScript.DoString(code, codeFriendlyName: RelativePath);
                }
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
                ScriptManager.Instance._currentExecutingModId = previousModId;
            }
        }

    }

    private sealed class ScriptMeta
    {
        public string? Overwrite { get; set; }
        public string? AddTo { get; set; }
        public string? Library { get; set; }
    }
}
