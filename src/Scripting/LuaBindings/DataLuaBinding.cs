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
            gameDataTable["Get"] = (Func<string, object?>)((path) =>
                DataManager.Instance.GetData(currentModId(), path));

            gameDataTable["Set"] = (Action<string, object?>)((path, value) =>
                DataManager.Instance.SetData(currentModId(), path, value));

            // From/to other mod
            gameDataTable["GetFrom"] = (Func<string, string, object?>)((modId, path) =>
                DataManager.Instance.GetData(modId, path));

            gameDataTable["SetTo"] = (Action<string, string, object?>)((modId, path, value) =>
                 DataManager.Instance.SetData(modId, path, value));

            gameDataTable["CategoryFrom"] = (Func<string, string, Table>)((modId, category) =>
            {
                return GetCategoryTable(modId, category);
            });

            gameDataTable["Category"] = (Func<string, Table>)((category) =>
            {
                return GetCategoryTable(currentModId(), category);
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

            Table GetCategoryTable(string modId, string category)
            {
                var result = new Table(luaScript);
                var container = DataManager.Instance.TryGetContainer(modId);
                if (container == null)
                    return result;

                foreach (var path in container.GetPathsInCategory(category))
                {
                    var value = container.GetFlatData(path);

                    try
                    {
                        result[path] = DynValue.FromObject(luaScript, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GameData] Warning: Could not add path '{path}' from mod '{modId}' to Lua table: {ex.Message}");
                        // skip this value
                    }
                }

                return result;
            }

        }
    }
}
