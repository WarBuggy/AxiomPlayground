using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.Libraries;

/// <summary>
/// Manages published Lua libraries.
/// </summary>
public sealed class LibraryManager
{
    public static LibraryManager Instance { get; } = new();

    private readonly Dictionary<LibraryId, LibraryRecord> _libraries = [];
    private int _publishCounter = 0;

    private LibraryManager() { }

    /// <summary>
    /// Register (publish) a library.
    /// </summary>
    public void RegisterLibrary(
        string publishingModId,
        string libraryName,
        Table? libraryTable,
        string publishScriptPath,
        string luaText)
    {
        var id = new LibraryId(publishingModId, libraryName);

        if (_libraries.ContainsKey(id))
            throw new ScriptRuntimeException(
                $"[LibraryManager] Library '{id}' already published.");

        var record = new LibraryRecord(
            publishingModId,
            libraryName,
            libraryTable,
            publishScriptPath,
            ++_publishCounter,
            luaText
        );

        _libraries[id] = record;
    }

    /// <summary>
    /// Get a library by full name (modId.libraryName)
    /// </summary>
    public LibraryRecord Get(string fullName)
    {
        var id = LibraryId.Parse(fullName);

        if (!_libraries.TryGetValue(id, out var record))
            throw new ScriptRuntimeException($"[LibraryManager] Library '{fullName}' not found.");

        return record;
    }

    /// <summary>
    /// Patch a library: record which mod patched it.
    /// </summary>
    public void Patch(string fullName, string patchingModId)
    {
        var record = Get(fullName);
        record.AddPatch(patchingModId);
    }
}
