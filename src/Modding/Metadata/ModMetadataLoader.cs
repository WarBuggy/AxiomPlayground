using AxiomPlayground.Modding.Metadata.Model;

namespace AxiomPlayground.Modding.Metadata;

public static class ModMetadataLoader
{
    /// <summary>
    /// Loads all metadata for a mod folder and builds a validated ModInfo object.
    /// </summary>
    public static ModInfo LoadInfo(string modFolder)
    {
        var info = ModInfoParser.Parse(modFolder);

        // In the future:
        // var description = ModDescriptionParser.Parse(modFolder);
        // var patches = ModPatchNotesParser.Parse(modFolder);

        return info;
    }
}