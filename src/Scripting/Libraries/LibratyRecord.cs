using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.Libraries;

/// <summary>
/// Represents a published library record.
/// </summary>
public sealed class LibraryRecord(
    string publishingModId,
    string libraryName,
    Table? libraryTable,
    string publishScriptPath,
    int publishOrder,
    string luaText)
{
    public string PublishingModId { get; } = publishingModId;
    public string LibraryName { get; } = libraryName;

    /// <summary>
    /// Lua table of the library. Can be null initially for text-based libraries.
    /// </summary>
    public Table? LibraryTable { get; set; } = libraryTable;

    /// <summary>
    /// The Lua script file path (optional).
    /// </summary>
    public string PublishScriptPath { get; } = publishScriptPath;

    /// <summary>
    /// The Lua text content of the library script.
    /// </summary>
    public string LuaText { get; } = luaText ?? throw new ArgumentNullException(nameof(luaText));

    /// <summary>
    /// Publish order counter
    /// </summary>
    public int PublishOrder { get; } = publishOrder;

    private readonly List<string> _patchingModIds = new();
    public IReadOnlyList<string> PatchingModIds => _patchingModIds;

    public void AddPatch(string patchingModId)
    {
        if (!_patchingModIds.Contains(patchingModId, StringComparer.OrdinalIgnoreCase))
            _patchingModIds.Add(patchingModId);
    }
}
