using MoonSharp.Interpreter;
using AxiomPlayground.Data;

namespace AxiomPlayground.Scripting.LuaBindings
{
    public sealed class DataLuaBinding : LuaBindingBase
    {
        public override void Register(Script luaScript)
        {
            ArgumentNullException.ThrowIfNull(luaScript);

            var gameDataTable = new Table(luaScript);

            gameDataTable["TryGet"] = (Func<string, DynValue, DynValue>)((path, modIdDyn) =>
            {
                if (!ResolveModId(modIdDyn, out var modId))
                    return DynValue.NewTuple(DynValue.Nil, DynValue.False);

                bool exists = DataManager.Instance.TryGetData(modId, path, out object? value);

                return DynValue.NewTuple(
                    value != null ? DynValue.FromObject(luaScript, value) : DynValue.Nil,
                    DynValue.NewBoolean(exists)
                );
            });

            gameDataTable["Set"] = (Action<string, object?, DynValue>)((path, value, modIdDyn) =>
            {
                if (!TryGetExecutingMod(out string actingModId))
                    return;

                string owningModId;
                if (modIdDyn.Type == DataType.String && !string.IsNullOrEmpty(modIdDyn.String))
                    owningModId = modIdDyn.String;
                else
                    owningModId = actingModId;

                DataManager.Instance.SetData(owningModId, path, actingModId, value);
            });

            #region FrameworkGameFlag.Debug

            gameDataTable["PathHistory"] = (Action<string, DynValue>)((path, modIdDyn) =>
            {

                if (!ResolveModId(modIdDyn, out var actingModId))
                    return;

                DataManager.Instance.ShowPathHistory(actingModId, path);
            });

            gameDataTable["AllPathHistories"] = (Action<DynValue>)((modIdDyn) =>
            {
                if (!ResolveModId(modIdDyn, out var actingModId))
                    return;

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
