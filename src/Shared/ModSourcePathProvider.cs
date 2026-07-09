namespace AxiomPlayground.Shared;

public static class ModSourcePathProvider
{
    private static readonly Dictionary<ModSource, string> _fallbackPaths =
        new()
        {
            {
                ModSource.Local,
                "Mods/"
            },
            {
                ModSource.Steam,
                "Steam/workshop/content/gameId/"
            }
        };

    private static Dictionary<ModSource, string>? _paths;

    public static string GetPath(ModSource source)
    {
        EnsureLoaded();

        if (_paths!.TryGetValue(source, out var path))
            return path;

        throw new InvalidOperationException(
            $"No path configured for mod source {source}");
    }

    private static void EnsureLoaded()
    {
        if (_paths != null)
            return;

        _paths = LoadFromConfig();

        foreach (var fallback in _fallbackPaths)
        {
            if (!_paths.ContainsKey(fallback.Key))
            {
                _paths[fallback.Key] = fallback.Value;
            }
        }
    }

    private static Dictionary<ModSource, string> LoadFromConfig()
    {
        // TODO:
        // Read json/config file here
        return [];
    }
}