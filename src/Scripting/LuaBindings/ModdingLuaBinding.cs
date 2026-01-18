using MoonSharp.Interpreter;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Scripting.LuaBindings
{
    public sealed class ModdingLuaBinding : LuaBindingBase
    {
        public override void Register(Script luaScript)
        {
            ArgumentNullException.ThrowIfNull(luaScript);

            // Helper to get the currently executing mod
            static string GetCurrentModId() => ScriptManager.Instance.CurrentExecutingModId;
            static Mod? GetCurrentMod() =>
                ModManager.Instance.TryGetMod(GetCurrentModId(), out var mod)
                    ? mod
                    : null;

            // Create a Lua table to hold mod-related functions
            var modTable = new Table(luaScript);

            // Precompute the list of enabled mod IDs
            var modIds = new Table(luaScript);
            int index = 1;
            foreach (var mod in ModManager.Instance.FinalModList)
            {
                modIds[index++] = mod.ModId;
            }
            modTable["Ids"] = (Func<Table>)(() => modIds);

            modTable["SetRuntimeFor"] = (Action<string, string, DynValue>)((modId, key, value) =>
            {
                if (!ModManager.Instance.TryGetMod(modId, out var mod)) return;
                var actingModId = GetCurrentModId();
                if (string.IsNullOrEmpty(actingModId)) return;
                mod.SetRuntimeData(actingModId, key, value);
            });

            modTable["RuntimeFrom"] = (Func<string, string, DynValue>)((modId, key) =>
            {
                if (!ModManager.Instance.TryGetMod(modId, out var mod)) return DynValue.Nil;
                var value = mod.GetRuntimeData(key);
                return value as DynValue ?? DynValue.Nil;
            });

            modTable["HasRuntimeFrom"] = (Func<string, string, bool>)((modId, key) =>
            {
                if (!ModManager.Instance.TryGetMod(modId, out var mod)) return false;
                return mod.TryGetRuntimeData(key, out _);
            });

            modTable["RemoveRuntimeFrom"] = (Func<string, string, bool>)((modId, key) =>
            {
                if (!ModManager.Instance.TryGetMod(modId, out var mod)) return false;
                var actingModId = GetCurrentModId();
                if (string.IsNullOrEmpty(actingModId)) return false;
                return mod.RemoveRuntimeData(actingModId, key);
            });

            modTable["ClearRuntimeFor"] = (Action<string>)((modId) =>
            {
                if (!ModManager.Instance.TryGetMod(modId, out var mod)) return;
                var actingModId = GetCurrentModId();
                if (string.IsNullOrEmpty(actingModId)) return;
                mod.ClearRuntimeData(actingModId);
            });

            modTable["SetRuntime"] = (Action<string, DynValue>)((key, value) =>
            {
                var mod = GetCurrentMod();
                if (mod == null) return;
                mod.SetRuntimeData(mod.ModId, key, value);
            });

            modTable["Runtime"] = (Func<string, DynValue>)((key) =>
            {
                var mod = GetCurrentMod();
                if (mod == null) return DynValue.Nil;
                var value = mod.GetRuntimeData(key);
                return value as DynValue ?? DynValue.Nil;
            });

            modTable["HasRuntime"] = (Func<string, bool>)((key) =>
            {
                var mod = GetCurrentMod();
                if (mod == null) return false;
                return mod.TryGetRuntimeData(key, out _);
            });

            modTable["RemoveRuntime"] = (Func<string, bool>)((key) =>
            {
                var mod = GetCurrentMod();
                if (mod == null) return false;
                return mod.RemoveRuntimeData(mod.ModId, key);
            });

            modTable["ClearRuntime"] = (Action)(() =>
            {
                var mod = GetCurrentMod();
                if (mod == null) return;
                mod.ClearRuntimeData(mod.ModId);
            });

            #region FrameworkGameFlag.Debug

            modTable["RuntimeHistoryFor"] = (Action<string, string>)((modId, key) =>
            {
                ModManager.Instance.ShowRuntimeHistory(modId, key);
            });

            modTable["AllRuntimeHistoriesFor"] = (Action<string>)(modId =>
            {
                ModManager.Instance.ShowAllRuntimeHistories(modId);
            });

            modTable["RuntimeHistory"] = (Action<string>)(key =>
            {
                var actingModId = GetCurrentModId();
                if (string.IsNullOrEmpty(actingModId)) return;
                ModManager.Instance.ShowRuntimeHistory(actingModId, key);
            });

            modTable["AllRuntimeHistories"] = (Action)(() =>
            {
                var actingModId = GetCurrentModId();
                if (string.IsNullOrEmpty(actingModId)) return;
                ModManager.Instance.ShowAllRuntimeHistories(actingModId);
            });

            modTable["AllRuntimeHistoriesForAllMods"] = (Action)(() =>
            {
                ModManager.Instance.ShowAllRuntimeHistoriesForAllMods();
            });
            #endregion

            // Register table in Lua globals
            luaScript.Globals["Mods"] = modTable;
        }
    }
}
