using System.Reflection;

namespace AxiomPlayground.Data;

public abstract class BaseManager(string categoryName, bool requiredProcessedPaths = false, string? processedCategoryName = null)
{

    public string CategoryName { get; } = categoryName;
    private static readonly string DEFAULT_PROCESSED_PREFIX = "Processed";
    public readonly bool RequiredProcessedPaths = requiredProcessedPaths;
    public string ProcessedCategoryName { get; } =
        processedCategoryName ?? categoryName + DEFAULT_PROCESSED_PREFIX;

    public virtual Dictionary<string, Dictionary<string, object?>> ProcessPathData
    (
        IReadOnlyList<CategoryData> collectedCategoryDataList,
        out Dictionary<string, Dictionary<string, PathHistory>> processedHistory
    )
    {
        processedHistory = [];
        return [];
    }

    public virtual IEnumerable<LoadEventDispatch> CollectLoadEvents()
    {
        yield break;
    }

    public string CreateFullPath(params string[] elements)
    {
        string prefix = RequiredProcessedPaths ? ProcessedCategoryName : CategoryName;
        if (elements == null || elements.Length == 0)
            return prefix;

        return prefix + "." + string.Join(".", elements);
    }

    public static string[] PrependPath(string[] original, params string[] prefix)
    {
        if (prefix == null || prefix.Length == 0) return original;
        if (original == null || original.Length == 0) return prefix;
        return [.. prefix, .. original];
    }

    public static string[] AppendPath(string[] original, params string[] suffix)
    {
        if (original == null || original.Length == 0) return suffix ?? [];
        if (suffix == null || suffix.Length == 0) return original;
        return [.. original, .. suffix];
    }

    public static IReadOnlyList<BaseManager> DiscoverManagers()
    {
        var baseType = typeof(BaseManager);

        return [.. AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null)!;
                }
            })
            .Where(t => t != null && !t.IsAbstract && baseType.IsAssignableFrom(t))
            .Select(t => GetManagerInstance(t!))
            .Where(m => m != null)
            .Cast<BaseManager>()];
    }

    private static BaseManager? GetManagerInstance(Type type)
    {
        var prop = type.GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static);

        if (prop == null)
            return null;

        if (!typeof(BaseManager).IsAssignableFrom(prop.PropertyType))
            return null;

        return prop.GetValue(null) as BaseManager;
    }
}

public readonly record struct LoadEventDispatch
(
    string EventName,
    string ModId,
    IReadOnlyList<object?> Args
);