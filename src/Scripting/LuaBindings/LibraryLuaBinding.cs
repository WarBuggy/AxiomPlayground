using MoonSharp.Interpreter;
using AxiomPlayground.Scripting.Libraries;

namespace AxiomPlayground.Scripting.LuaBindings;

/// <summary>
/// Provides the `Library` global in Lua for publishing, patching, and retrieving libraries.
/// </summary>
public sealed class LibraryLuaBinding(string currentModId)
{
    private readonly string _currentModId = currentModId;

    public void Register(Script luaScript)
    {
        var libTable = new Table(luaScript);

        // Get a library by full name (modId.LibraryName)
        libTable["Get"] = (Func<string, Table>)(fullName =>
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ScriptRuntimeException("[LibraryLuaBinding] fullName cannot be null or empty.");

            // Fetch the library record
            var record = LibraryManager.Instance.Get(fullName);

            // If the table is already cached, return it
            if (record.LibraryTable != null)
                return record.LibraryTable;

            // Otherwise, execute the Lua text in this mod's Script instance
            DynValue result = luaScript.DoString(
                record.LuaText,
                codeFriendlyName: record.PublishScriptPath ?? (record.PublishingModId + record.LibraryName)
            );

            // The chunk should return a table
            if (result.Type != DataType.Table || result.Table == null)
                throw new ScriptRuntimeException(
                    $"[LibraryLuaBinding] '{record.PublishingModId}.{record.LibraryName}' did not return a table."
                );

            // Cache the table in the library record
            record.LibraryTable = result.Table;

            // Inject into current mod globals
            luaScript.Globals[record.LibraryName] = result.Table;

            return result.Table;
        });

        // Patch: record which mod patched the library
        libTable["Patch"] = (Action<string, DynValue>)((fullName, patchFn) =>
        {
            if (patchFn.Type != DataType.Function)
                throw new ScriptRuntimeException("[LibraryLuaBinding] Patch must be a function.");

            LibraryManager.Instance.Patch(fullName, _currentModId);
            patchFn.Function.Call();
        });

        // Expose the `Library` table in Lua globals
        luaScript.Globals["Library"] = libTable;
    }
}
