using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class DefinitionLuaBinding : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        var definitionTable = new Table(luaScript);

        definitionTable["TryGetPayload"] = (Func<string, string, DynValue, DynValue, DynValue>)(
            (typeName, defName, pathTable, modIdDyn) =>
        {
            if (!ResolveModId(modIdDyn, out var modId))
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            if (pathTable.Type != DataType.Table)
                return DynValue.NewTuple(DynValue.Nil, DynValue.False);

            var pathParts = ConvertLuaTableToStringList(pathTable);

            bool exists = DefinitionManager.Instance.TryGetPayload(
                modId,
                typeName,
                defName,
                pathParts,
                out object? value
            );

            return DynValue.NewTuple(
                value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                DynValue.NewBoolean(exists)
            );
        });

        definitionTable["SetPayload"] = (Action<string, string, DynValue, object?, DynValue>)(
            (typeName, defName, pathTable, value, modIdDyn) =>
        {
            if (!TryGetExecutingMod(out string actingModId))
                return;

            string owningModId;
            if (modIdDyn.Type == DataType.String && !string.IsNullOrEmpty(modIdDyn.String))
                owningModId = modIdDyn.String;
            else
                owningModId = actingModId;

            if (pathTable.Type != DataType.Table)
                return;

            var pathParts = ConvertLuaTableToStringList(pathTable);

            DefinitionManager.Instance.SetPayload(
                owningModId,
                typeName,
                defName,
                pathParts,
                actingModId,
                value
            );
        });

        definitionTable["TryCreatePayload"] = (Func<string, string, DynValue, object?, DynValue, DynValue>)(
            (typeName, defName, pathTable, value, modIdDyn) =>
        {
            if (!TryGetExecutingMod(out string actingModId))
                return DynValue.False;

            string owningModId;
            if (modIdDyn.Type == DataType.String && !string.IsNullOrEmpty(modIdDyn.String))
                owningModId = modIdDyn.String;
            else
                owningModId = actingModId;

            if (pathTable.Type != DataType.Table)
                return DynValue.False;

            var pathParts = ConvertLuaTableToStringList(pathTable);

            bool success = DefinitionManager.Instance.TryCreatePayload(
                owningModId,
                typeName,
                defName,
                pathParts,
                actingModId,
                value
            );

            return DynValue.NewBoolean(success);
        });

        luaScript.Globals["Definition"] = definitionTable;
    }
}