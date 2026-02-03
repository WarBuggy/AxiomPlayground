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
        ledgerArrayTable["Insert"] = (Func<LedgerArray, int, object, int>)((ledger, luaIndex, value) =>
        {
            var actorId = currentModId();
            return ledger.InsertAt(luaIndex, value, actorId);
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

        // TryInsertBeforeValue / TryInsertAfterValue
        ledgerArrayTable["TryInsertBeforeValue"] = (Func<LedgerArray, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertBeforeValue(existingValue, newValue, actorId, out int newIndex))
                return DynValue.NewNumber(newIndex);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryInsertAfterValue"] = (Func<LedgerArray, object, object, DynValue>)((ledger, existingValue, newValue) =>
        {
            var actorId = currentModId();
            if (ledger.TryInsertAfterValue(existingValue, newValue, actorId, out int newIndex))
                return DynValue.NewNumber(newIndex);
            return DynValue.Nil;
        });

        // Remove
        ledgerArrayTable["TryRemove"] = (Func<LedgerArray, int, bool>)((ledger, luaIndex) =>
        {
            var actorId = currentModId();
            return ledger.TryRemove(luaIndex, actorId);
        });

        // Clear
        ledgerArrayTable["Clear"] = (Action<LedgerArray>)(ledger =>
        {
            var actorId = currentModId();
            ledger.Clear(actorId);
        });

        // Getters
        ledgerArrayTable["TryGet"] = (Func<LedgerArray, int, DynValue>)((ledger, luaIndex) =>
        {
            if (ledger.TryGet(luaIndex, out object value))
                return DynValue.FromObject(luaScript, value);
            return DynValue.Nil;
        });

        ledgerArrayTable["TryGetWithOwner"] = (Func<LedgerArray, int, DynValue>)((ledger, luaIndex) =>
        {
            if (ledger.TryGet(luaIndex, out object? value, out string ownerId))
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
            int idx = ledger.GetIndexOf(value);
            return idx >= 0 ? idx : 0; // 0 = not found in Lua
        });

        // Set value
        ledgerArrayTable["Set"] = (Action<LedgerArray, int, object>)((ledger, luaIndex, newValue) =>
        {
            var actorId = currentModId();
            ledger.Set(luaIndex, newValue, actorId);
        });

        // Count
        ledgerArrayTable["Count"] = (Func<LedgerArray, int>)(ledger => ledger.Count);

        // Iterators over values
        ledgerArrayTable["Iterator"] = (Func<LedgerArray, DynValue>)(ledger =>
        {
            var keys = ledger.Keys.ToList();
            int idx = 0;
            return DynValue.NewCallback((ctx, args) =>
            {
                if (idx >= keys.Count) return DynValue.Nil;

                string key = keys[idx++];
                ledger.TryGet(key, out var value);
                return DynValue.FromObject(luaScript, value);
            });
        });

        // Iterators over values + owner
        ledgerArrayTable["IteratorWithOwner"] = (Func<LedgerArray, DynValue>)(ledger =>
        {
            var keys = ledger.Keys.ToList();
            int idx = 0;
            return DynValue.NewCallback((ctx, args) =>
            {
                if (idx >= keys.Count) return DynValue.Nil;

                string key = keys[idx++];
                if (ledger.TryGet(key, out var value, out var owner))
                {
                    var tbl = new Table(luaScript);
                    tbl["Value"] = DynValue.FromObject(luaScript, value);
                    tbl["Owner"] = owner;
                    return DynValue.NewTable(tbl);
                }

                return DynValue.Nil;
            });
        });

        luaScript.Globals["LedgerArray"] = ledgerArrayTable;
    }
}
