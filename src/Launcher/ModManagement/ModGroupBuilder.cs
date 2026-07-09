using AxiomPlayground.Shared;
using AxiomPlayground.Modding;

namespace Launcher.ModManagement;

public static class ModGroupBuilder
{
    public static List<ModGroup> Build(IEnumerable<Mod> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);

        var groups = new Dictionary<string, ModGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods)
        {
            // RESERVED RULE: only allow Local source
            if (ModSystemPolicy.RESERVED_MOD_IDS.Contains(mod.Info.Id) &&
                mod.Source != ModSource.Local)
            {
                continue; // ignore invalid source
            }

            if (!groups.TryGetValue(mod.Info.Id, out var group))
            {
                group = new ModGroup(mod.Info.Id);
                groups.Add(mod.Info.Id, group);
            }

            group.Add(mod);
        }

        return [.. groups.Values];
    }
}