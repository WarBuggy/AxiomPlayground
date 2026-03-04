using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public abstract class LuaBindingBase
{
    /// <summary>
    /// Each binding class implements this to register its functions to Lua.
    /// </summary>
    public abstract void Register(Script luaScript);

    // Helper to get current executing mod
    public static bool TryGetExecutingMod(out string modId)
    {
        modId = ScriptManager.Instance.CurrentExecutingModId;
        return !string.IsNullOrWhiteSpace(modId);
    }

    public static bool ResolveModId(DynValue modIdDyn, out string modId)
    {
        if (modIdDyn.Type == DataType.String && !string.IsNullOrEmpty(modIdDyn.String))
        {
            modId = modIdDyn.String;
            return true;
        }

        return TryGetExecutingMod(out modId);
    }

    public static List<string> ConvertLuaTableToStringList(DynValue tableDyn)
    {
        var list = new List<string>();
        if (tableDyn.Type != DataType.Table || tableDyn.Table == null)
            return list;

        for (int i = 1; i <= tableDyn.Table.Length; i++)
        {
            var value = tableDyn.Table.Get(i);
            if (value.Type == DataType.String)
                list.Add(value.String);
        }

        return list;
    }
}