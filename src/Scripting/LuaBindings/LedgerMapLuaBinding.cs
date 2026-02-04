using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class LedgerMapLuaBinding : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        // Register the LedgerMap type for MoonSharp
        UserData.RegisterType<LedgerMap>();

        static string currentModId() => ScriptManager.Instance.CurrentExecutingModId;

        var ledgerMapTable = new Table(luaScript);

        // Factory
        ledgerMapTable["Create"] = (Func<LedgerMap>)(() => new LedgerMap());

        // Set a key-value pair
        ledgerMapTable["Set"] = (Action<LedgerMap, string, object>)((ledger, key, value) =>
        {
            var actorId = currentModId();
            ledger.Set(key, value, actorId);
        });

        // TryGet value
        ledgerMapTable["TryGet"] = (Func<LedgerMap, string, DynValue>)((ledger, key) =>
        {
            if (ledger.TryGet(key, out object? value))
            {
                return DynValue.NewTuple(
                    DynValue.FromObject(luaScript, value),
                    DynValue.True
                );
            }

            return DynValue.NewTuple(DynValue.Nil, DynValue.False);
        });

        // TryGet value with owner
        ledgerMapTable["TryGetWithOwner"] = (Func<LedgerMap, string, DynValue>)((ledger, key) =>
        {
            if (ledger.TryGet(key, out object? value, out string owner))
            {
                return DynValue.NewTuple(
                    DynValue.FromObject(luaScript, value),
                    DynValue.NewString(owner),
                    DynValue.True
                );
            }

            return DynValue.NewTuple(
                DynValue.Nil,
                DynValue.Nil,
                DynValue.False
            );
        });

        // Remove a key
        ledgerMapTable["TryRemove"] = (Func<LedgerMap, string, bool>)((ledger, key) =>
        {
            var actorId = currentModId();
            return ledger.TryRemove(key, actorId);
        });

        // Clear all keys
        ledgerMapTable["Clear"] = (Action<LedgerMap>)(ledger =>
        {
            var actorId = currentModId();
            ledger.Clear(actorId);
        });

        // IndexOf by value (returns the key if found)
        ledgerMapTable["IndexOf"] = (Func<LedgerMap, object, string?>)((ledger, value) =>
        {
            return ledger.GetKeyOf(value);
        });

        // Count
        ledgerMapTable["Count"] = (Func<LedgerMap, int>)(ledger => ledger.Count);

        // Iterator over all key-value pairs
        ledgerMapTable["Iterator"] = (Func<LedgerMap, DynValue>)(ledger =>
        {
            var keys = ledger.Keys.ToList();
            int index = 0;
            return DynValue.NewCallback((ctx, args) =>
            {
                if (index >= keys.Count) return DynValue.Nil;

                string key = keys[index++];
                ledger.TryGet(key, out object? value);
                var tbl = new Table(luaScript);
                tbl["Key"] = key;
                tbl["Value"] = DynValue.FromObject(luaScript, value);
                return DynValue.NewTable(tbl);
            });
        });

        // Iterator over key-value pairs with owner
        ledgerMapTable["IteratorWithOwner"] = (Func<LedgerMap, DynValue>)(ledger =>
        {
            var keys = ledger.Keys.ToList();
            int index = 0;
            return DynValue.NewCallback((ctx, args) =>
            {
                if (index >= keys.Count) return DynValue.Nil;

                string key = keys[index++];
                if (ledger.TryGet(key, out object? value, out string owner))
                {
                    var tbl = new Table(luaScript);
                    tbl["Key"] = key;
                    tbl["Value"] = DynValue.FromObject(luaScript, value);
                    tbl["Owner"] = owner;
                    return DynValue.NewTable(tbl);
                }

                return DynValue.Nil;
            });
        });

        luaScript.Globals["LedgerMap"] = ledgerMapTable;
    }
}
