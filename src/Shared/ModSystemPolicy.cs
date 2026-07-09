namespace AxiomPlayground.Shared;

public static class ModSystemPolicy
{
    public const string CORE_MOD_ID = "Core";

    public static readonly List<string> RESERVED_MOD_IDS =
    [
        CORE_MOD_ID,
        "DLC1",
        "DLC2",
        "DLC3"
    ];

    public static readonly HashSet<string> RESERVED_SET =
        new(StringComparer.OrdinalIgnoreCase)
        {
            CORE_MOD_ID,
            "DLC1",
            "DLC2",
            "DLC3"
        };

    public static readonly string SelectedModFilePath =
        Path.Combine(AppContext.BaseDirectory, "launchModList.json");
}