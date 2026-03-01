using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Scripting;

/// <summary>
/// Custom MoonSharp script loader that resolves require() calls
/// relative to the currently executing mod's Scripts/ folder.
/// </summary>
public sealed class ModAwareScriptLoader : ScriptLoaderBase
{
    private const string SCRIPTS_FOLDER = "Scripts";

    public ModAwareScriptLoader()
    {
        IgnoreLuaPathGlobal = true;
    }

    public override object LoadFile(string file, Table globalContext)
    {
        return File.ReadAllText(file);
    }

    public override bool ScriptFileExists(string name)
    {
        return File.Exists(name);
    }

    public override string ResolveModuleName(string modname, Table globalContext)
    {
        try
        {
            string modId = ScriptManager.Instance.CurrentExecutingModId;
            string modFolder = ModManager.Instance.GetModFolderPath(modId);

            // Normalize forward slashes to OS separator and append .lua
            string normalized = modname.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(modFolder, SCRIPTS_FOLDER, normalized + ".lua");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ModAwareScriptLoader] Failed to resolve '{modname}': {ex.Message}");
            return modname + ".lua";
        }
    }
}
