using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class DefinitionLuaBinding : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        static string currentModId() => ScriptManager.Instance.CurrentExecutingModId;

        var definitionTable = new Table(luaScript);

        // Own mod 

        definitionTable["TryGet"] = (Func<string, string, DynValue>)((defName, propertyName) =>
        {
            var modId = currentModId();
            if (string.IsNullOrWhiteSpace(modId))
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            bool exists = DefinitionManager.Instance.TryGet(modId, defName, propertyName, out object? value);

            return DynValue.NewTuple(
                value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                DynValue.NewBoolean(exists)
            );
        });

        definitionTable["Set"] = (Action<string, string, object?>)((defName, propertyName, value) =>
        {
            var modId = currentModId();
            if (string.IsNullOrWhiteSpace(modId))
                return;

            DefinitionManager.Instance.Set(modId, defName, propertyName, value, modId);
        });

        definitionTable["TryGetType"] = (Func<string, DynValue>)((defName) =>
        {
            var modId = currentModId();
            if (string.IsNullOrWhiteSpace(modId))
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            bool exists = DefinitionManager.Instance.TryGetType(modId, defName, out var type);

            return DynValue.NewTuple(
                type != null ? DynValue.NewString(type) : DynValue.Nil,
                DynValue.NewBoolean(exists)
            );
        });

        definitionTable["Exists"] = (Func<string, bool>)((defName) =>
        {
            var modId = currentModId();
            if (string.IsNullOrWhiteSpace(modId))
                return false;

            return DefinitionManager.Instance.Exists(modId, defName);
        });

        definitionTable["GetAll"] = (Func<Table>)(() =>
        {
            var modId = currentModId();
            var table = new Table(luaScript);

            if (string.IsNullOrWhiteSpace(modId))
                return table;

            var defs = DefinitionManager.Instance.GetDefinitions(modId);
            for (int i = 0; i < defs.Count; i++)
                table[i + 1] = defs[i];

            return table;
        });

        definitionTable["GetByType"] = (Func<string, Table>)((type) =>
        {
            var modId = currentModId();
            var table = new Table(luaScript);

            if (string.IsNullOrWhiteSpace(modId))
                return table;

            var defs = DefinitionManager.Instance.GetDefinitionsByType(modId, type);
            for (int i = 0; i < defs.Count; i++)
                table[i + 1] = defs[i];

            return table;
        });

        // Cross-mod 

        definitionTable["TryGetFrom"] = (Func<string, string, string, DynValue>)((modId, defName, propertyName) =>
        {
            if (string.IsNullOrWhiteSpace(modId))
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            object? value;
            bool exists = DefinitionManager.Instance.TryGet(modId, defName, propertyName, out value);

            return DynValue.NewTuple(
                value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                DynValue.NewBoolean(exists)
            );
        });

        definitionTable["TryGetTypeFrom"] = (Func<string, string, DynValue>)((modId, defName) =>
        {
            if (string.IsNullOrWhiteSpace(modId))
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            bool exists = DefinitionManager.Instance.TryGetType(modId, defName, out var type);

            return DynValue.NewTuple(
                type != null ? DynValue.NewString(type) : DynValue.Nil,
                DynValue.NewBoolean(exists)
            );
        });

        definitionTable["ExistsIn"] = (Func<string, string, bool>)((modId, defName) =>
        {
            if (string.IsNullOrWhiteSpace(modId))
                return false;

            return DefinitionManager.Instance.Exists(modId, defName);
        });

        definitionTable["SetTo"] = (Action<string, string, string, object?>)((modId, defName, propertyName, value) =>
        {
            var actingModId = currentModId();
            if (string.IsNullOrWhiteSpace(actingModId))
                return;

            DefinitionManager.Instance.Set(modId, defName, propertyName, value, actingModId);
        });

        luaScript.Globals["Definition"] = definitionTable;
    }
}