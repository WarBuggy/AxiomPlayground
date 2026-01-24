using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings
{
    public sealed class DataLuaBinding : LuaBindingBase
    {
        public override void Register(Script luaScript)
        {
            ArgumentNullException.ThrowIfNull(luaScript);

            static string currentModId() => ScriptManager.Instance.CurrentExecutingModId;

            var gameDataTable = new Table(luaScript);

            // From own mod
            gameDataTable["TryGet"] = (Func<string, DynValue>)((path) =>
            {
                var modId = currentModId();
                if (string.IsNullOrWhiteSpace(modId))
                    return DynValue.NewTuple(DynValue.Nil, DynValue.False);

                object? value;
                bool exists = DataManager.Instance.TryGetData(modId, path, out value);

                return DynValue.NewTuple(
                    value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                    DynValue.NewBoolean(exists)
                );
            });

            gameDataTable["Set"] = (Action<string, object?>)((path, value) =>
                DataManager.Instance.SetData(currentModId(), path, value, currentModId()));

            // From/to other mod
            gameDataTable["TryGetFrom"] = (Func<string, string, DynValue>)((modId, path) =>
            {
                if (string.IsNullOrWhiteSpace(modId))
                    return DynValue.NewTuple(DynValue.Nil, DynValue.False);

                object? value;
                bool exists = DataManager.Instance.TryGetData(modId, path, out value);

                return DynValue.NewTuple(
                    value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                    DynValue.NewBoolean(exists)
                );
            });

            gameDataTable["SetTo"] = (Action<string, string, object?>)((modId, path, value) =>
                 DataManager.Instance.SetData(modId, path, value, currentModId()));

            #region FrameworkGameFlag.Debug

            gameDataTable["PathHistoryFor"] = (Action<string, string>)((modId, path) =>
            {
                DataManager.Instance.ShowPathHistory(modId, path);
            });

            gameDataTable["AllPathHistoriesFor"] = (Action<string>)(modId =>
            {
                DataManager.Instance.ShowAllPathHistories(modId);
            });

            gameDataTable["PathHistory"] = (Action<string>)(path =>
            {
                var actingModId = currentModId();
                if (string.IsNullOrEmpty(actingModId)) return;

                DataManager.Instance.ShowPathHistory(actingModId, path);
            });

            gameDataTable["AllPathHistories"] = (Action)(() =>
            {
                var actingModId = currentModId();
                if (string.IsNullOrEmpty(actingModId)) return;

                DataManager.Instance.ShowAllPathHistories(actingModId);
            });

            gameDataTable["AllPathHistoriesForAllMods"] = (Action)(() =>
            {
                DataManager.Instance.ShowAllPathHistoriesForAllMods();
            });

            #endregion

            luaScript.Globals["GameData"] = gameDataTable;
        }
    }
}
