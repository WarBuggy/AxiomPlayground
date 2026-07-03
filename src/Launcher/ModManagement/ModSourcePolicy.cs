namespace Launcher.ModManagement;

using AxiomPlayground.Modding;

public static class ModSourcePolicy
{
    /// <summary>
    /// Defines the order in which ModSources are displayed in the UI.
    /// First entry is also the default fallback if available.
    /// </summary>
    public static readonly ModSource[] DisplayOrder =
    [
        ModSource.Steam,
        ModSource.Local
    ];

    /// <summary>
    /// Returns true if the given source is considered the default
    /// when multiple sources exist for a ModGroup.
    /// </summary>
    public static ModSource GetDefaultSource(IEnumerable<ModSource> availableSources)
    {
        ArgumentNullException.ThrowIfNull(availableSources);

        var set = availableSources.ToHashSet();

        foreach (var source in DisplayOrder)
        {
            if (set.Contains(source))
                return source;
        }

        throw new InvalidOperationException(Shared.T("errorModSourcePolicyInvalidSource"));
    }

    /// <summary>
    /// Returns sources sorted according to UI display priority.
    /// </summary>
    public static IEnumerable<ModSource> Sort(IEnumerable<ModSource> sources)
    {
        var set = sources.ToHashSet();

        foreach (var source in DisplayOrder)
        {
            if (set.Contains(source))
                yield return source;
        }

        // In case future sources exist that are not in DisplayOrder
        foreach (var source in sources)
        {
            if (!DisplayOrder.Contains(source))
                yield return source;
        }
    }

    /// <summary>
    /// Checks whether a given source is the first available according to policy.
    /// Useful for preselecting radio buttons in UI.
    /// </summary>
    public static bool IsDefault(ModSource source, IEnumerable<ModSource> availableSources)
    {
        return GetDefaultSource(availableSources) == source;
    }
}