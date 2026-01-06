using MoonSharp.Interpreter;
using AxiomPlayground.Localization;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class LocalizationLuaBindings : LuaBindingBase
{
    public override void Register(Script luaScript)
    {
        ArgumentNullException.ThrowIfNull(luaScript);

        // Define all functions to bind
        var bindings = new[]
        {
            new { Name = "LocalizeWithEndingFrom", RequiredArgs = 3, Func = (Func<string[], object[], string>)((args, fmt) =>
                StringUtils.LocalizeWithEndingFrom(args[0], args[1], args[2], fmt)) },

            new { Name = "LocalizeFrom", RequiredArgs = 2, Func = (Func<string[], object[], string>)((args, fmt) =>
                StringUtils.LocalizeWithEndingFrom(args[0], ".", args[1], fmt)) },

            new { Name = "Localize", RequiredArgs = 1, Func = (Func<string[], object[], string>)((args, fmt) =>
            {
                string currentModId = ScriptManager.Instance.CurrentExecutingModId;
                return StringUtils.LocalizeWithEndingFrom(currentModId, ".", args[0], fmt);
            }) },

            new { Name = "LocalizeWithEnding", RequiredArgs = 2, Func = (Func<string[], object[], string>)((args, fmt) =>
            {
                string currentModId = ScriptManager.Instance.CurrentExecutingModId;
                return StringUtils.LocalizeWithEndingFrom(currentModId, args[0], args[1], fmt);
            }) },
        };

        // Register all bindings in Lua
        foreach (var b in bindings)
        {
            luaScript.Globals[b.Name] = (Func<CallbackArguments, string>)(args =>
            {
                if (args.Count < b.RequiredArgs)
                    throw new ScriptRuntimeException($"[LocalizationLuaBindings] {b.Name} requires at least {b.RequiredArgs} arguments.");

                string[] fixedArgs = ExtractStringArgs(args, b.RequiredArgs);
                object[] formatArgs = ExtractFormatArgs(args, b.RequiredArgs);
                return b.Func(fixedArgs, formatArgs);
            });
        }
    }

    private static string[] ExtractStringArgs(CallbackArguments args, int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++)
            arr[i] = args[i].ToObject() as string ?? "";
        return arr;
    }

    private static object[] ExtractFormatArgs(CallbackArguments args, int startIndex)
    {
        var formatArgsList = new List<object>();
        for (int i = startIndex; i < args.Count; i++)
            formatArgsList.Add(args[i].ToObject());
        return [.. formatArgsList];
    }
}
