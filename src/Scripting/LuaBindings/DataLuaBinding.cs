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


            gameDataTable["TryCreate"] = (Func<string, object?, DynValue>)((path, value) =>
            {
                var owningModId = currentModId();
                var actingModId = owningModId;
                if (string.IsNullOrEmpty(owningModId))
                    return DynValue.NewTuple(DynValue.NewBoolean(false), DynValue.NewString("Current mod ID is null or empty"));

                if (DataManager.Instance.TryCreateData(owningModId, path, value, actingModId, out var error))
                    return DynValue.NewTuple(DynValue.NewBoolean(true), DynValue.Nil);

                return DynValue.NewTuple(DynValue.NewBoolean(false), DynValue.NewString(error ?? "<unknown error>"));
            });

            gameDataTable["TryCreateFor"] = (Func<string, string, object?, DynValue>)((owningModId, path, value) =>
            {
                var actingModId = currentModId();
                if (string.IsNullOrWhiteSpace(actingModId))
                    return DynValue.NewTuple(DynValue.NewBoolean(false), DynValue.NewString("Acting mod ID is null or empty"));

                if (DataManager.Instance.TryCreateData(owningModId, path, value, actingModId, out var error))
                    return DynValue.NewTuple(DynValue.NewBoolean(true), DynValue.Nil);

                return DynValue.NewTuple(DynValue.NewBoolean(false), DynValue.NewString(error ?? "<unknown error>"));
            });

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
