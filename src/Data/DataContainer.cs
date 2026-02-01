namespace AxiomPlayground.Data;

public sealed class DataContainer(bool frameworkDebugEnabled)
{
    private readonly Dictionary<string, object?> _flatData = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _frameworkDebugEnabled = frameworkDebugEnabled;
    private readonly Dictionary<string, PathHistory> _pathHistory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _categoryIndex = new(StringComparer.OrdinalIgnoreCase);

    #region Functions for loading data from JSON files

    public void HandleFlatData
    (
        string writerModId,
        Dictionary<string, object?> flattenedData,
        bool isOverwrite,
        string? category = null
    )
    {
        foreach (var pv in flattenedData)
        {
            var path = pv.Key;
            var value = pv.Value;
            TryAddToFlatData(writerModId, path, value, isOverwrite, category);
        }
    }

    private void TryAddToFlatData
    (
        string writerModId,
        string path,
        object? value,
        bool isOverwrite,
        string? category
    )
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        bool hasChildren = _flatData.Keys.Any(k => k.StartsWith(path + ".", StringComparison.OrdinalIgnoreCase));
        if (hasChildren)
        {
            Console.WriteLine($"[DataContainer] Skipping '{path}' because children exist.");
            return;
        }

        // Check for exact path
        if (_flatData.ContainsKey(path))
        {
            if (!isOverwrite)
            {
                Console.WriteLine($"[DataContainer] Skipping '{path}' because exact path exists and overwrite not allowed.");
                return;
            }

            // Overwrite allowed
            _flatData[path] = value;
            RegisterPath(path, category);
            RecordWrite(path, writerModId, existed: true, value);
            return;
        }

        // No exact path and no children → safe to add
        // Check parents for primitives
        var segments = path.Split('.');
        string prefix = "";
        for (int i = 0; i < segments.Length - 1; i++)
        {
            prefix = i == 0 ? segments[0] : prefix + "." + segments[i];

            if (_flatData.TryGetValue(prefix, out var parentValue))
            {
                // Delete the primitive parent to ensure all paths are leaf nodes
                _flatData.Remove(prefix);
                UnregisterPath(prefix);
                RecordDelete(prefix, writerModId, parentValue, [path]);
                Console.WriteLine($"[DataContainer] Deleted parent primitive '{prefix}' to insert '{path}'.");
                break; // Only one deletion should be needed
            }
        }

        _flatData[path] = value;
        RegisterPath(path, category);
        RecordWrite(path, writerModId, existed: false, value);
    }

    private void RegisterPath(string path, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        if (!_categoryIndex.TryGetValue(category, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _categoryIndex[category] = set;
        }

        set.Add(path);
    }

    private void UnregisterPath(string path)
    {
        foreach (var set in _categoryIndex.Values)
            set.Remove(path);
    }

    public IReadOnlyCollection<string> GetPathsInCategory(string category)
    {
        return _categoryIndex.TryGetValue(category, out var set)
            ? set
            : Array.Empty<string>();
    }

    public bool TryGetFlatData(string path, out object? value)
    {
        return _flatData.TryGetValue(path, out value);
    }

    public void ClearCategoryIndex(string category)
    {
        _categoryIndex.Remove(category);
    }

    #endregion

    #region Functions for manipulate path values during runtime

    public void SetFlatData(string owningModId, string path, object? value, string actingModId)
    {
        if (!_flatData.ContainsKey(path))
        {
            throw new InvalidOperationException
            (
                $"[DataContainer] Cannot set '{path}' for owning mod '{owningModId}' at runtime because the path does not exist. " +
                "All paths must be created in JSON data files before runtime."
            );
        }

        _flatData[path] = value!;

        RecordWrite(path, actingModId, existed: true, value);
    }

    public bool TryCreateFlatData(string owningModId, string path, object? value, string actingModId, out string? error)
    {
        error = null;

        // Path must not already exist
        if (_flatData.ContainsKey(path))
        {
            error = $"Cannot create '{path}' for owning mod '{owningModId}' because the path already exists.";
            return false;
        }

        // Reject if children already exist
        if (_flatData.Keys.Any(k => k.StartsWith(path + ".", StringComparison.OrdinalIgnoreCase)))
        {
            error = $"Cannot create '{path}' because child paths already exist.";
            return false;
        }

        // Reject if any parent exists
        var segments = path.Split('.');
        string prefix = "";
        for (int i = 0; i < segments.Length - 1; i++)
        {
            prefix = i == 0 ? segments[0] : prefix + "." + segments[i];

            if (_flatData.ContainsKey(prefix))
            {
                error = $"Cannot create '{path}' because parent path '{prefix}' already exists.";
                return false;
            }
        }

        // Create the new path
        _flatData[path] = value;
        RecordWrite(path, actingModId, existed: false, value);

        // Auto-detect category from first segment (no new category allowed here)
        string firstSegment = segments[0];
        if (_categoryIndex.ContainsKey(firstSegment))
            RegisterPath(path, firstSegment);

        return true;
    }


    #endregion

    #region FrameworkGameFlag.Debug 

    private PathHistory GetOrCreateHistory(string path)
    {
        if (!_pathHistory.TryGetValue(path, out var history))
        {
            history = new();
            _pathHistory[path] = history;
        }

        return history;
    }

    private void RecordWrite(string path, string modId, bool existed, object? value)
    {
        if (!_frameworkDebugEnabled) return;

        var history = GetOrCreateHistory(path);
        if (existed)
            history.AddOverwrite(modId, value);
        else
            history.AddCreate(modId, value);
    }

    private void RecordDelete(string deletedPath, string modId, object? value, List<string> causedByPath)
    {
        if (!_frameworkDebugEnabled) return;

        GetOrCreateHistory(deletedPath).AddDeleted(modId, value, causedByPath);
    }

    public bool TryGetPathHistory(string path, out PathHistory? history)
    {
        if (!_frameworkDebugEnabled)
        {
            history = null!;
            Console.WriteLine("[DataContainer] Framework debug mode is not enabled. History is not recorded.");
            return false;
        }

        if (_pathHistory.TryGetValue(path, out var original))
        {
            history = new PathHistory(original);
            return true;
        }

        history = null;
        return false;
    }

    public void AddProcessedHistory(IReadOnlyDictionary<string, PathHistory> processedHistory)
    {
        if (!_frameworkDebugEnabled)
        {
            Console.WriteLine("[DataContainer] Framework debug mode is not enabled. Processed history is ignored.");
            return;
        }

        foreach (var (icomingPath, incomingHistory) in processedHistory)
        {
            _pathHistory[icomingPath] = incomingHistory;
        }
    }

    public void PrintHistoryList(string path)
    {
        if (!_frameworkDebugEnabled)
        {
            Console.WriteLine("[DataContainer] Framework debug mode is not enabled. History is not recorded.");
            return;
        }

        if (!_pathHistory.TryGetValue(path, out var history))
        {
            Console.WriteLine
            (
                $"[DataContainer] No history found for path '{path}'. " +
                "The path has never been created or written by any mod."
            );
            return;
        }

        history.Print(path);
    }

    public void PrintAllHistoryList(string modId)
    {
        if (!_frameworkDebugEnabled)
        {
            Console.WriteLine("[DataContainer] Framework debug mode is not enabled. History is not recorded.");
            return;
        }

        if (_pathHistory.Count == 0)
        {
            Console.WriteLine("[DataContainer] No paths with history to print.");
            return;
        }

        Console.WriteLine($"[DataContainer] Printing history for all paths of mod '{modId}':");

        foreach (var kvp in _pathHistory.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var path = kvp.Key;
            var history = kvp.Value;
            history.Print(path);
            Console.WriteLine(); // extra line between paths for readability
        }
    }

    #endregion
}
