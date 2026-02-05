using MoonSharp.Interpreter;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Scripting.LuaBindings
{
    public sealed class ModdingLuaBinding : LuaBindingBase
    {
        public override void Register(Script luaScript)
        {
            ArgumentNullException.ThrowIfNull(luaScript);

            var modTable = new Table(luaScript);

            // Precompute the list of enabled mod IDs
            var modIds = new Table(luaScript);
            int index = 1;
            foreach (var mod in ModManager.Instance.FinalModList)
            {
                modIds[index++] = mod.ModId;
            }
            modTable["Ids"] = (Func<Table>)(() => modIds);

            modTable["Current"] = (Func<string>)(() =>
            {
                return ScriptManager.Instance.CurrentExecutingModId ?? "Unknown";
            });

            luaScript.Globals["Mods"] = modTable;
        }
    }
}
