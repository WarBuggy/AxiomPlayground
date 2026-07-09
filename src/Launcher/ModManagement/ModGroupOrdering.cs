using AxiomPlayground.Shared;

namespace Launcher.ModManagement;

public static class ModGroupOrdering
{
    public static List<ModGroup> Order(List<ModGroup> groups)
    {
        var result = new List<ModGroup>();

        var groupLookup = groups.ToDictionary(
            g => g.ModId,
            StringComparer.OrdinalIgnoreCase);

        // 1. Reserved mods first (in defined order)
        foreach (var reservedId in ModSystemPolicy.RESERVED_MOD_IDS)
        {
            if (groupLookup.TryGetValue(reservedId, out var group))
            {
                result.Add(group);
            }
        }

        // 2. All non-reserved mods
        foreach (var group in groups)
        {
            if (!ModSystemPolicy.RESERVED_MOD_IDS.Contains(
                    group.ModId,
                    StringComparer.OrdinalIgnoreCase))
            {
                result.Add(group);
            }
        }

        return result;
    }
}