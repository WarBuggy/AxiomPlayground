using System.Diagnostics.Contracts;
using System.Net;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Data;

/// <summary>
/// Base class for all managers that load data from DataManager by category.
/// Handles iteration over all paths in a category and cleans up the category index automatically.
/// </summary>
public abstract class BaseManager(string categoryName)
{
    // Optional: category name if needed
    public string CategoryName { get; } = categoryName;

    /// <summary>
    /// Load all data for this manager from DataManager for the provided mods.
    /// Automatically cleans up the category index after processing.
    /// </summary>
    /// <param name="mods">List of mods to load data from.</param>
    public void LoadAll(List<ModInstance> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException($"[{GetType().Name}] No mods provided for loading category '{CategoryName}'.");

        foreach (var mod in mods)
        {
            LoadForMod(mod);
        }

        // Cleanup after processing: remove the category index
        DataManager.Instance.ClearCategoryIndex(CategoryName);
    }

    /// <summary>
    /// Process all data in this category for a single mod.
    /// </summary>
    /// <param name="mod">The mod to process.</param>
    private void LoadForMod(ModInstance mod)
    {
        DataContainer? container = DataManager.Instance.TryGetContainer(mod.ModId);
        if (container == null) return;

        var pathsInCategory = container.GetPathsInCategory(CategoryName);
        foreach (var path in pathsInCategory)
        {
            object? value = container.GetFlatData(path);
            try
            {
                ProcessPath(mod.ModId, path, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{GetType().Name}] Error processing path '{path}' in mod '{mod.ModId}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Called for each path in the category.
    /// Derived classes implement this to apply their custom logic for each path.
    /// </summary>
    /// <param name="modId">The mod ID this path belongs to.</param>
    /// <param name="path">The flattened path string.</param>
    /// <param name="value">The value at the path (may be null).</param>
    protected abstract void ProcessPath(string modId, string path, object? value);
}