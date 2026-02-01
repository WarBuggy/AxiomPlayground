using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class LedgerArrayLuaBinding : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        // Register the LedgerArray type for MoonSharp
        UserData.RegisterType<LedgerArray>();

        static string currentModId() => ScriptManager.Instance.CurrentExecutingModId;

        var ledgerArrayTable = new Table(luaScript);

        // Factory
        ledgerArrayTable["Create"] = (Func<LedgerArray>)(() => new LedgerArray());

        // Insert operations (Lua index is 1-based)
        ledgerArrayTable["InsertAt"] = (Func<LedgerArray, int, object, int>)((ledger, luaIndex, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertAt(luaIndex - 1, value, actorId); // adjust to 0-based
        });

        ledgerArrayTable["InsertFirst"] = (Func<LedgerArray, object, int>)((ledger, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertFirst(value, actorId);
        });

        ledgerArrayTable["InsertLast"] = (Func<LedgerArray, object, int>)((ledger, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertLast(value, actorId);
        });

        // TryInsertBefore/After
        ledgerArrayTable["TryInsertBefore"] = (Func<LedgerArray, int, object, DynValue>)((ledger, existingItemId, value) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertBefore(existingItemId, value, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertAfter"] = (Func<LedgerArray, int, object, DynValue>)((ledger, existingItemId, value) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertAfter(existingItemId, value, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertBeforeValue"] = (Func<LedgerArray, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertBeforeValue(existingValue, newValue, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertAfterValue"] = (Func<LedgerArray, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertAfterValue(existingValue, newValue, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        // Remove (Lua index is 1-based)
        ledgerArrayTable["TryRemoveAt"] = (Func<LedgerArray, int, bool>)((ledger, luaIndex) =>
        {
            var actorId = currentModId();
            return ledger.TryRemoveAt(luaIndex - 1, actorId);
        });

        // Clear
        ledgerArrayTable["Clear"] = (Action<LedgerArray>)(ledger =>
        {
            var actorId = currentModId();
            ledger.Clear(actorId);
        });

        // Getters
        ledgerArrayTable["TryGetAt"] = (Func<LedgerArray, int, DynValue>)((ledger, luaIndex) =>
        {
            if (ledger.TryGetAt(luaIndex - 1, out object value))
                return DynValue.FromObject(luaScript, value);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryGetAtWithOwner"] = (Func<LedgerArray, int, DynValue>)((ledger, luaIndex) =>
        {
            if (ledger.TryGetAt(luaIndex - 1, out object value, out string ownerId))
            {
                var tbl = new Table(luaScript);
                tbl["Value"] = DynValue.FromObject(luaScript, value);
                tbl["Owner"] = ownerId;
                return DynValue.NewTable(tbl);
            }
            return DynValue.Nil;
        });

        // IndexOf (Lua-friendly 1-based)
        ledgerArrayTable["IndexOf"] = (Func<LedgerArray, object, int>)((ledger, value) =>
        {
            int idx = ledger.IndexOf(value);
            return idx >= 0 ? idx + 1 : 0; // 0 = not found
        });

        // Update value
        ledgerArrayTable["SetValueAt"] = (Action<LedgerArray, int, object>)((ledger, luaIndex, newValue) =>
        {
            var actorId = currentModId();
            ledger.SetValueAt(luaIndex - 1, newValue, actorId);
        });

        // Count
        ledgerArrayTable["Count"] = (Func<LedgerArray, int>)(ledger => ledger.Count);

        luaScript.Globals["LedgerArray"] = ledgerArrayTable;
    }
}
