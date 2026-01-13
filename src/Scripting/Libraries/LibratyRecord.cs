using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.Libraries;

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
    public Table? LibraryTable { get; set; } = libraryTable;
    public string PublishScriptPath { get; } = publishScriptPath;
    public string LuaText { get; } = luaText ?? throw new ArgumentNullException(nameof(luaText));
    public int PublishOrder { get; } = publishOrder;
    private readonly Dictionary<string, List<(string reltivePath, string luaText)>> _patchScriptsByMod = [];
    public IReadOnlyDictionary<string, List<(string reltivePath, string luaText)>> PatchScriptsByMod => _patchScriptsByMod;

    public void AddPatch(string patchingModId, string relativePath, string luaText)
    {
        if (string.IsNullOrWhiteSpace(patchingModId))
            throw new ArgumentException("[LibraryRecord] patchingModId cannot be null or empty", nameof(patchingModId));

        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("[LibraryRecord] relativePath cannot be null or empty", nameof(relativePath));

        if (string.IsNullOrWhiteSpace(luaText))

            throw new ArgumentException("[LibraryRecord] filePath cannot be null or empty", nameof(luaText));

        if (!_patchScriptsByMod.TryGetValue(patchingModId, out var list))
        {
            list = [];
            _patchScriptsByMod[patchingModId] = list;
        }

        list.Add((relativePath, luaText));
    }
}
