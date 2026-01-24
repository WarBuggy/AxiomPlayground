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
        throw new NotImplementedException();
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
}