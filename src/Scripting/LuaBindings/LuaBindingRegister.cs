using System.Reflection;
using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public static class LuaBindingRegistrar
{
    private const string BINDINGS_NAMESPACE = "AxiomPlayground.Scripting.LuaBindings";

    public static void RegisterAllBindings(Script luaScript)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var bindingTypes = assembly.GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.IsSubclassOf(typeof(LuaBindingBase))
                        && t.Namespace == BINDINGS_NAMESPACE);

        foreach (var type in bindingTypes)
        {
            // Create instance and call Register
            var instance = (LuaBindingBase)Activator.CreateInstance(type)!;
            instance.Register(luaScript);
        }
    }
}
