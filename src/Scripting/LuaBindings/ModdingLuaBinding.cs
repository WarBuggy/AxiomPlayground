using MoonSharp.Interpreter;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Scripting.LuaBindings
{
    public sealed class ModdingLuaBinding : LuaBindingBase
    {
        public override void Register(Script luaScript)
        {
            ArgumentNullException.ThrowIfNull(luaScript);

            // Create a Lua table to hold mod-related functions
            var modTable = new Table(luaScript);

            // Precompute the list of enabled mod IDs
            var modIds = new Table(luaScript);
            int index = 1;
            foreach (var mod in ModManager.Instance.FinalModList)
            {
                modIds[index] = mod.ModId;
                index++;
            }

            // Function simply returns the precomputed table
            modTable["Ids"] = (Func<Table>)(() => modIds);

            luaScript.Globals["Mods"] = modTable;
        }
    }
}
