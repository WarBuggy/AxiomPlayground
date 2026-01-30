using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class LedgerArrayLuaBinding : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        UserData.RegisterType<LedgerArray<object>>();

        static string currentModId() => ScriptManager.Instance.CurrentExecutingModId;

        var ledgerArrayTable = new Table(luaScript);

        // Factory
        ledgerArrayTable["Create"] = (Func<LedgerArray<object>>)(() => new LedgerArray<object>());

        // Insert operations (Lua index is 1-based)
        ledgerArrayTable["InsertAt"] = (Func<LedgerArray<object>, int, object, int>)((ledger, luaIndex, value) =>
        {
            var actorId = currentModId();
            int itemId = ledger.InsertAt(luaIndex - 1, value, actorId); // adjust to 0-based
            return itemId;
        });

        ledgerArrayTable["InsertFirst"] = (Func<LedgerArray<object>, object, int>)((ledger, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertFirst(value, actorId);
        });

        ledgerArrayTable["InsertLast"] = (Func<LedgerArray<object>, object, int>)((ledger, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertLast(value, actorId);
        });

        // TryInsertBefore/After: indices still use 0-based internally, but Lua sees 1-based?
        ledgerArrayTable["TryInsertBefore"] = (Func<LedgerArray<object>, int, object, DynValue>)((ledger, existingItemId, value) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertBefore(existingItemId, value, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertAfter"] = (Func<LedgerArray<object>, int, object, DynValue>)((ledger, existingItemId, value) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertAfter(existingItemId, value, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertBeforeValue"] = (Func<LedgerArray<object>, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertBeforeValue(existingValue, newValue, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertAfterValue"] = (Func<LedgerArray<object>, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertAfterValue(existingValue, newValue, actorId, out int newItemId))
                return DynValue.NewNumber(newItemId);
            return DynValue.Nil;
        });

        // Remove (adjust Lua index -> 0-based)
        ledgerArrayTable["TryRemoveAt"] = (Func<LedgerArray<object>, int, bool>)((ledger, luaIndex) =>
        {
            var actorId = currentModId();
            return ledger.TryRemoveAt(luaIndex - 1, actorId);
        });

        // Clear
        ledgerArrayTable["Clear"] = (Action<LedgerArray<object>>)((ledger) =>
        {
            var actorId = currentModId();
            ledger.Clear(actorId);
        });

        // Getters
        ledgerArrayTable["TryGetAt"] = (Func<LedgerArray<object>, int, DynValue>)((ledger, luaIndex) =>
        {
            if (ledger.TryGetAt(luaIndex - 1, out object value))
                return DynValue.FromObject(luaScript, value);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryGetAtWithOwner"] = (Func<LedgerArray<object>, int, DynValue>)((ledger, luaIndex) =>
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

        // IndexOf (return Lua-friendly 1-based index)
        ledgerArrayTable["IndexOf"] = (Func<LedgerArray<object>, object, int>)((ledger, value) =>
        {
            int idx = ledger.IndexOf(value);
            return idx >= 0 ? idx + 1 : 0; // 0 means not found in Lua
        });

        // Update value
        ledgerArrayTable["SetValueAt"] = (Action<LedgerArray<object>, int, object>)((ledger, luaIndex, newValue) =>
        {
            var actorId = currentModId();
            ledger.SetValueAt(luaIndex - 1, newValue, actorId);
        });

        ledgerArrayTable["Count"] = (Func<LedgerArray<object>, int>)((ledger) =>
        {
            return ledger.Count;
        });

        luaScript.Globals["LedgerArray"] = ledgerArrayTable;
    }
}