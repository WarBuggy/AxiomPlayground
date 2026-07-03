using AxiomPlayground.Modding.Metadata;

namespace AxiomPlayground.Modding.Discovery;

public sealed class ModDiscovery
{
    private readonly Dictionary<ModSource, string> _sourcePaths = new()
    {
        { ModSource.Steam, "Steam/workshop/content/gameId/" },
        { ModSource.Local, "Mods/" }
    };

    public List<Mod> Discover()
    {
        var result = new List<Mod>();

        foreach (var (source, rootPath) in _sourcePaths)
        {
            var modsFromSource = ScanSource(source, rootPath);
            result.AddRange(modsFromSource);
        }

        return result;
    }

    private static List<Mod> ScanSource(ModSource source, string rootPath)
    {
        var mods = new List<Mod>();

        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"[ModDiscovery] Folder not found: {rootPath}");
            return mods;
        }

        foreach (var modFolder in Directory.GetDirectories(rootPath))
        {
            try
            {
                var mod = LoadModFromFolder(modFolder, source);
                if (mod != null)
                    mods.Add(mod);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ModDiscovery] Skipping '{modFolder}': {ex.Message}");
            }
        }

        return mods;
    }

    private static Mod? LoadModFromFolder(string modFolder, ModSource source)
    {
        // Delegate ALL parsing + validation to metadata layer
        var info = ModMetadataLoader.LoadInfo(modFolder);

        // If metadata loader throws → folder is invalid
        // (we do not validate files here anymore)

        var mod = new Mod(info, source);

        return mod;
    }
}