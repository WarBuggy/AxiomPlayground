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

            // From own mod
            luaScript.Globals["GetData"] = (Func<string, object?>)((path) =>
                DataManager.Instance.GetData(currentModId(), path));

            luaScript.Globals["SetData"] = (Action<string, object?>)((path, value) =>
                DataManager.Instance.SetData(currentModId(), path, value));

            // From/to other mod
            luaScript.Globals["GetDataFrom"] = (Func<string, string, object?>)((modId, path) =>
                DataManager.Instance.GetData(modId, path));

            luaScript.Globals["SetDataTo"] = (Action<string, string, object?>)((modId, path, value) =>
                DataManager.Instance.SetData(modId, path, value));
        }
    }
}
