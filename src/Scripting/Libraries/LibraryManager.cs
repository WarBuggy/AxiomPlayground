using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.Libraries;

public sealed class LibraryManager
{
    private static readonly LibraryManager _instance = new();
    public static LibraryManager Instance => _instance;

    private readonly Dictionary<string, LibraryRecord> _libraries = new(StringComparer.OrdinalIgnoreCase);
    private int _publishCounter = 0;

    private LibraryManager() { }

    public void RegisterLibrary(
        string publishingModId,
        string libraryName,
        Table? libraryTable,
        string publishScriptPath,
        string luaText)
    {
        if (_libraries.ContainsKey(libraryName))
            throw new ScriptRuntimeException($"[LibraryManager] Library '{libraryName}' already published.");

        var record = new LibraryRecord(
            publishingModId,
            libraryName,
            libraryTable,
            publishScriptPath,
            ++_publishCounter,
            luaText
        );

        _libraries[libraryName] = record;
        Console.WriteLine($"[LibraryManager] Library registered: {libraryName} from '{publishingModId}', '{publishScriptPath}'.");
    }

    public void AddLibraryPatch(string patchingModId, string libraryName, string relativePath, string luaText)
    {
        if (!_libraries.TryGetValue(libraryName, out var record))
            throw new ScriptRuntimeException($"[LibraryManager] Library '{libraryName}' not found.");

        record.AddPatch(patchingModId, relativePath, luaText);

        Console.WriteLine(
            $"[LibraryManager] Patch registered: {patchingModId} patched {libraryName} with '{relativePath}'.");
    }

    public IReadOnlyList<LibraryRecord> GetAllLibraries() => [.. _libraries.Values];

    public bool CheckNameExists(string name) => _libraries.ContainsKey(name);
}

