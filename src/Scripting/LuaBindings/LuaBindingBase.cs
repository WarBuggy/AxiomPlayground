using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public abstract class LuaBindingBase
{
    /// <summary>
    /// Each binding class implements this to register its functions to Lua.
    /// </summary>
    public abstract void Register(Script luaScript);
}