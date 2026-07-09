using BootstrapLocalization = AxiomPlayground.Shared.Shared;
using AxiomPlayground.Shared;
using AxiomPlayground.Modding.Metadata;


namespace AxiomPlayground.Modding.Discovery;

public sealed class ModDiscovery
{
    public static List<Mod> Discover()
    {
        var result = new List<Mod>();

        foreach (ModSource source in Enum.GetValues<ModSource>())
        {
            string rootPath = ModSourcePathProvider.GetPath(source);

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
            Console.WriteLine(
                BootstrapLocalization.T("errorModDiscoverFolderNotFound", rootPath));
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
                    BootstrapLocalization.T("errorModDiscoverSkippingFolder", modFolder, ex.Message));
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

    public static Mod LoadSelectedMod(ModSelectedState selected)
    {
        ArgumentNullException.ThrowIfNull(selected);

        string rootPath = ModSourcePathProvider.GetPath(selected.Source);

        string modFolder = Path.Combine(rootPath, selected.ModId);

        Mod? mod = LoadModFromFolder(modFolder, selected.Source) ??
            throw new DirectoryNotFoundException(BootstrapLocalization.T(
                "errorModDiscoverSelectedModNotFound", selected.ModId, selected.Source));

        if (!mod.Info.Id.Equals(selected.ModId, StringComparison.OrdinalIgnoreCase))
            throw new Exception(BootstrapLocalization.T(
                "errorModDiscoverSelectedModIdMismatch", selected.ModId, mod.Info.Id));

        return mod;
    }
}