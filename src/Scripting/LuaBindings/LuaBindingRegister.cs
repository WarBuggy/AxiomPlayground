using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public static class LuaBindingRegistrar
{
    public static List<string> RegisterAllBindings(
    Script luaScript,
    IEnumerable<Type> bindingTypes)
    {
        List<string> registered = [];

        foreach (var type in bindingTypes)
        {
            var instance = (LuaBindingBase)Activator.CreateInstance(type)!;
            instance.Register(luaScript);

            registered.Add(type.FullName ?? type.Name);
        }

        return registered;
    }
}
