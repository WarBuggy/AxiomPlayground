using System.Reflection;
using System.Text;

namespace AxiomPlayground.Data;

public abstract class BaseManager(string categoryName, bool requiredProcessedPaths = false)
{

    public string CategoryName { get; } = categoryName;
    public readonly bool RequiredProcessedPaths = requiredProcessedPaths;

    public virtual void ProcessPathData(IReadOnlyList<CategoryData> collectedCategoryDataList) { }

    public virtual IEnumerable<LoadEventDispatch> CollectLoadEvents() { yield break; }
    public virtual void CleanupAfterLoadEvents() { }
    public string CreateFullPath(params string[] elements)
    {
        if (elements == null || elements.Length == 0)
            return CategoryName;

        var sb = new StringBuilder();

        sb.Append(CategoryName);

        foreach (var element in elements)
        {
            sb.Append('.');
            sb.Append(element);
        }

        return sb.ToString();
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