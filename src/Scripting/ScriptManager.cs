using System.Reflection;
using System.Text.Json;
using MoonSharp.Interpreter;
using AxiomPlayground.Modding;
using AxiomPlayground.Scripting.LuaBindings;
using System.Data;

namespace AxiomPlayground.Scripting;

public sealed class ScriptManager
{
    private static readonly ScriptManager _instance = new();
    public static ScriptManager Instance => _instance;

    private const string SCRIPT_FOLDER = "Scripts";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private string? _currentExecutingModId;
    public string CurrentExecutingModId => _currentExecutingModId ?? "Unknown";

    private readonly List<Type> _luaBindingTypes;
    private readonly LuaEventBus _eventBus = new();
    private readonly Script _luaScript = new();

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

    public List<ScriptQueueItem> LoadAll(List<Mod> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[ScriptManager] No mods provided.");

        List<ScriptQueueItem> queue = [];
        var validModIds = mods.Select(m => m.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods)
        {
            string scriptsRoot = Path.Combine(ModManager.Instance.GetModFolderPath(mod), SCRIPT_FOLDER);
            if (!Directory.Exists(scriptsRoot))
                continue;

            // If init.lua exists, only auto-execute it (module-based mod).
            // Otherwise, auto-execute all .lua files alphabetically (legacy).
            string initPath = Path.Combine(scriptsRoot, "init.lua");
            IEnumerable<string> luaFiles = File.Exists(initPath)
                ? [initPath]
                : Directory.GetFiles(scriptsRoot, "*.lua", SearchOption.AllDirectories).Order();

            foreach (var luaFileFullPath in luaFiles)
            {
                string relativePath = Path.GetRelativePath(scriptsRoot, luaFileFullPath).Replace("\\", "/");
                string metaPath = Path.ChangeExtension(luaFileFullPath, ".json");

                ScriptMeta? meta = null;
                if (File.Exists(metaPath))
                {
                    try
                    {
                        meta = JsonSerializer.Deserialize<ScriptMeta>(File.ReadAllText(metaPath), _jsonSerializerOptions);
                    }
                    catch { meta = null; }
                }

                var queueItem = CreateQueueItem(meta, mod.ModId, relativePath, luaFileFullPath);
                InsertQueueItem(queueItem, queue);
            }
        }
        return queue;
    }

    private static ScriptQueueItem CreateQueueItem(ScriptMeta? meta, string modId, string relativePath, string fullPath)
    {
        ScriptOperation operation = ScriptOperation.None;
        string? targetModId = null;
        string? targetFile = null;
        bool execute = true;

        // Check if metadata exists
        if (meta != null)
        {
            // If TargetFile is missing, mark as not executable
            if (string.IsNullOrEmpty(meta.TargetFile))
            {
                Console.WriteLine($"[ScriptManager] {modId} Warning: '{relativePath}' metadata is malformed (targetFile missing). Skipping.");
                execute = false;
            }
            else
            {
                targetFile = meta.TargetFile;

                // Determine operation based on priority: Append > Prepend > Overwrite
                if (!string.IsNullOrEmpty(meta.Append))
                {
                    operation = ScriptOperation.Append;
                    targetModId = meta.Append;
                }
                else if (!string.IsNullOrEmpty(meta.Prepend))
                {
                    operation = ScriptOperation.Prepend;
                    targetModId = meta.Prepend;
                }
                else if (!string.IsNullOrEmpty(meta.Overwrite))
                {
                    operation = ScriptOperation.Overwrite;
                    targetModId = meta.Overwrite;
                }
                else
                {
                    // Metadata exists but no operation key → not executable
                    Console.WriteLine($"[ScriptManager] {modId} Warning: '{relativePath}' metadata has targetFile but no operation (append/prepend/overwrite). Skipping.");
                    execute = false;
                }
            }
        }

        return new ScriptQueueItem
        {
            ModId = modId,
            RelativePath = relativePath,
            FullPath = fullPath,
            Operation = operation,
            TargetModId = targetModId,
            TargetFile = targetFile,
            Execute = execute
        };
    }

    private static void InsertQueueItem(ScriptQueueItem item, List<ScriptQueueItem> queue)
    {
        if (!item.Execute || item.Operation == ScriptOperation.None)
        {
            // Non-executable or normal file → just add to the end
            queue.Add(item);

            if (item.Execute)
                Console.WriteLine($"[ScriptManager] {item.ModId}: '{item.RelativePath}' is added to execution queue.");

            return;
        }
        // Normal file, just add to the end
        if (item.Operation == ScriptOperation.None || string.IsNullOrEmpty(item.TargetFile))
        {
            int newIndex = queue.Count;
            queue.Add(item);

        }

        int targetIndex = queue.FindIndex(q =>
            q.ModId == item.TargetModId && q.RelativePath == item.TargetFile);
        if (targetIndex == -1)
        {
            string operation = item.Operation.ToString().ToLowerInvariant();
            item.Execute = false;
            queue.Add(item);
            Console.WriteLine($"[ScriptManager] {item.ModId} Warning: Target '{item.TargetModId}', '{item.TargetFile}' not found for '{item.RelativePath}' ({operation}). Skipping.");
            return;
        }

        switch (item.Operation)
        {
            case ScriptOperation.Overwrite:
                HandleOverwrite(item, targetIndex, queue);
                break;

            case ScriptOperation.Prepend:
                queue.Insert(targetIndex, item);
                Console.WriteLine($"[ScriptManager] {item.ModId}: '{item.RelativePath}' prepended before '{item.TargetModId}', '{item.TargetFile}'.");
                break;

            case ScriptOperation.Append:
                queue.Insert(targetIndex + 1, item);
                Console.WriteLine($"[ScriptManager] {item.ModId}: '{item.RelativePath}' appended after '{item.TargetModId}', '{item.TargetFile}'.");
                break;

            case ScriptOperation.None:
            default:
                break;
        }
    }

    private static void HandleOverwrite(ScriptQueueItem item, int targetIndex, List<ScriptQueueItem> queue)
    {
        var targetItem = queue[targetIndex];

        // Check if target is still executable
        if (!targetItem.Execute)
        {
            Console.WriteLine($"[ScriptManager] {item.ModId} Warning: Overwrite target '{item.TargetModId}', '{item.TargetFile}' for '{item.RelativePath}' is already replaced. Skipping.");
            return;
        }

        // Mark the original file as not to execute
        targetItem.Execute = false;

        // Insert the new item right after the target
        int insertIndex = targetIndex + 1;
        queue.Insert(insertIndex, item);

        Console.WriteLine($"[ScriptManager] {item.ModId}: '{item.RelativePath}' successfully overwrote '{item.TargetModId}', '{item.TargetFile}'.");
    }

    public void ExecuteQueue(List<ScriptQueueItem> queue)
    {
        _luaScript.Options.ScriptLoader = new ModAwareScriptLoader();

        // Register all C# bindings
        LuaBindingRegistrar.RegisterAllBindings(_luaScript, _luaBindingTypes);
        new EventLuaBinding(_eventBus).Register(_luaScript);

        _luaScript.Globals["ExecutingModId"] = (Func<string>)(() => _currentExecutingModId ?? "Unknown");

        // Load engine-provided Lua utilities (available as globals to all mods)
        LoadEmbeddedLuaLib();

        foreach (var item in queue)
        {
            if (!item.Execute)
                continue;

            if (Modding.ModErrorTracker.Instance.IsModErrored(item.ModId))
                continue;

            var previousModId = _currentExecutingModId;
            _currentExecutingModId = item.ModId;
            try
            {
                string code = File.ReadAllText(item.FullPath);

                // Execute Lua code with chunk name = RelativePath
                _luaScript.DoString(code, codeFriendlyName: item.RelativePath);
            }
            catch (ScriptRuntimeException ex)
            {
                Modding.ModErrorTracker.Instance.MarkModErrored(
                    item.ModId, ex.DecoratedMessage, $"script '{item.RelativePath}'"
                );
            }
            catch (Exception ex)
            {
                Modding.ModErrorTracker.Instance.MarkModErrored(
                    item.ModId, ex.Message, $"script '{item.RelativePath}'"
                );
            }
            finally
            {
                // Restore previous executing mod
                _currentExecutingModId = previousModId;
            }
        }
    }

    private void LoadEmbeddedLuaLib()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "AxiomPlayground.Scripting.LuaLib.";

        foreach (var resourceName in assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".lua"))
            .Order())
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var code = reader.ReadToEnd();
            var friendlyName = resourceName[prefix.Length..];

            try
            {
                _luaScript.DoString(code, codeFriendlyName: friendlyName);
                Console.WriteLine($"[ScriptManager] Loaded engine utility: {friendlyName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptManager] Failed to load engine utility '{friendlyName}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tick engine-level Lua utilities (Timer, etc.) each frame.
    /// Called from the game loop before mod events.
    /// </summary>
    public void TickEngineCore(float deltaTime, float totalTime)
    {
        var fn = _luaScript.Globals.Get("__engineCoreUpdate");
        if (fn.Type == DataType.Function)
        {
            try
            {
                _luaScript.Call(fn, DynValue.NewNumber(deltaTime), DynValue.NewNumber(totalTime));
            }
            catch (ScriptRuntimeException ex)
            {
                Console.WriteLine($"[ScriptManager] Engine core loop error: {ex.DecoratedMessage}");
            }
        }
    }

    public void Fire(string eventName, params DynValue[] args)
    {
        // LuaEventBus is still used to store handlers
        _eventBus.TryGetHandlers(eventName, out var handlers);
        if (handlers == null)
            return;

        foreach (var kv in handlers)
        {
            if (Modding.ModErrorTracker.Instance.IsModErrored(kv.Key))
                continue;

            var previous = _currentExecutingModId;
            _currentExecutingModId = kv.Key;

            foreach (var fn in kv.Value)
            {
                try { fn.Call(args); }
                catch (ScriptRuntimeException ex)
                {
                    Modding.ModErrorTracker.Instance.MarkModErrored(
                        _currentExecutingModId,
                        ex.DecoratedMessage,
                        $"event '{eventName}'"
                    );
                    break;
                }
                catch (Exception ex)
                {
                    Modding.ModErrorTracker.Instance.MarkModErrored(
                        _currentExecutingModId,
                        ex.Message,
                        $"event '{eventName}'"
                    );
                    break;
                }
            }

            _currentExecutingModId = previous;
        }
    }

    public string? DoString(string code, string codeFriendlyName = "console")
    {
        try
        {
            var result = _luaScript.DoString(code, codeFriendlyName: codeFriendlyName);
            if (result == null || result.IsNil() || result.IsVoid())
                return null;
            return result.ToPrintString();
        }
        catch (ScriptRuntimeException ex)
        {
            return $"Runtime error: {ex.DecoratedMessage}";
        }
        catch (MoonSharp.Interpreter.SyntaxErrorException ex)
        {
            return $"Syntax error: {ex.DecoratedMessage}";
        }
    }

    public void CallWithModContext(string modId, Closure fn, params DynValue[] args)
    {
        if (Modding.ModErrorTracker.Instance.IsModErrored(modId)) return;

        var previous = _currentExecutingModId;
        _currentExecutingModId = modId;
        try { fn.Call(args); }
        catch (ScriptRuntimeException ex)
        {
            Modding.ModErrorTracker.Instance.MarkModErrored(modId, ex.DecoratedMessage, "scene callback");
        }
        catch (Exception ex)
        {
            Modding.ModErrorTracker.Instance.MarkModErrored(modId, ex.Message, "scene callback");
        }
        finally { _currentExecutingModId = previous; }
    }

    public DynValue[] BuildEventArgs(IEnumerable<object?> args)
    {
        return [.. args
            .Select(a => a == null
                ? DynValue.Nil
                : DynValue.FromObject(_luaScript, a))];
    }

    public sealed class ScriptQueueItem
    {
        public required string ModId { get; set; }
        public required string RelativePath { get; set; }
        public required string FullPath { get; set; }
        public ScriptOperation Operation { get; set; } = ScriptOperation.None;
        public string? TargetModId { get; set; }
        public string? TargetFile { get; set; }
        public bool Execute { get; set; } = true;
    }

    public enum ScriptOperation
    {
        None,
        Append,
        Prepend,
        Overwrite
    }

    private sealed class ScriptMeta
    {
        public string? Overwrite { get; set; }
        public string? Prepend { get; set; }
        public string? Append { get; set; }
        public string? TargetFile { get; set; }
    }
}
