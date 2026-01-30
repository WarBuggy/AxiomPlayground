namespace AxiomPlayground.Data;

public class DefinitionManager : BaseManager
{
    private static readonly DefinitionManager _instance = new();
    public static DefinitionManager Instance => _instance;
    private readonly Dictionary<string, HashSet<string>> _modDefinitions = [];
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _definitionTypes = [];
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _defPathsCache = [];

    private DefinitionManager() : base("definition", true, "definition") { }

    public override Dictionary<string, Dictionary<string, object?>> ProcessPathData
    (
        IReadOnlyList<CategoryData> collectedCategoryDataList,
        out Dictionary<string, Dictionary<string, PathHistory>> processedHistory
    )
    {
        foreach (var modData in collectedCategoryDataList)
        {
            string modId = modData.ModId;
            var definitions = modData.Values;

            if (!_modDefinitions.ContainsKey(modId))
                _modDefinitions[modId] = [];
            if (!_definitionTypes.ContainsKey(modId))
                _definitionTypes[modId] = [];
            if (!_defPathsCache.ContainsKey(modId))
                _defPathsCache[modId] = [];

            foreach (var pathAndValue in definitions)
            {
                var path = pathAndValue.Key;
                var pathParts = path.Split(".");

                if (pathParts.Length < 3)
                    continue;

                string defName = pathParts[1];

                // If this def is new, register it
                bool isNewDef = !_modDefinitions[modId].Contains(defName);
                if (isNewDef)
                {
                    _modDefinitions[modId].Add(defName);

                    // Handle Type
                    string typePath = $"{CategoryName}.{defName}.type";
                    if (definitions.TryGetValue(typePath, out var typeValue))
                    {
                        string? typeStr = typeValue as string;
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            if (!_definitionTypes[modId].TryGetValue(typeStr, out var defList))
                            {
                                defList = [];
                                _definitionTypes[modId][typeStr] = defList;
                            }
                            defList.Add(defName);
                        }
                    }

                    // Initialize path cache for this def
                    if (!_defPathsCache[modId].ContainsKey(defName))
                        _defPathsCache[modId][defName] = [];
                }

                // Add the current path to the def's path cache
                string prefix = $"{CategoryName}.{defName}.payload.";
                if (path.StartsWith(prefix))
                {
                    string relativePath = path[prefix.Length..];
                    if (!string.IsNullOrEmpty(relativePath))
                        _defPathsCache[modId][defName].Add(relativePath);
                }
            }
        }

        // return empty 
        processedHistory = [];
        return [];
    }

    public override IEnumerable<LoadEventDispatch> CollectLoadEvents()
    {
        foreach (var (modId, defNames) in _modDefinitions)
        {
            foreach (var defName in defNames)
            {
                string? type = null;

                if (_definitionTypes.TryGetValue(modId, out var typeMap))
                {
                    foreach (var kv in typeMap)
                    {
                        if (kv.Value.Contains(defName))
                        {
                            type = kv.Key;
                            break;
                        }
                    }
                }

                yield return new LoadEventDispatch
                (
                    "OnDefinitionCreated",
                    modId,
                    [modId, defName, type]
                );
            }
        }
    }

    public bool TryGetPayload(string modId, string defName, string propertyName, out object? value)
    {
        value = null;

        if (!_modDefinitions.TryGetValue(modId, out var defNames) || !defNames.Contains(defName))
            return false;

        // Build full path
        string fullPath = CreateFullPath([defName, "payload", propertyName]);

        if (DataManager.Instance.TryGetData(modId, fullPath, out value))
        {
            return true;
        }

        return false; // property does not exist
    }

    public bool TryGetType(string modId, string defName, out string? type)
    {
        string fullPath = CreateFullPath([defName, "type"]);

        if (DataManager.Instance.TryGetData(modId, fullPath, out var typeValue))
        {
            type = typeValue as string;
            return true;
        }
        type = null;
        return false;
    }

    public List<string> GetDefinitionsByType(string modId, string type)
    {
        if (_definitionTypes.TryGetValue(modId, out var typeDict) &&
            typeDict.TryGetValue(type, out var defList))
        {
            return [.. defList]; // return a copy to be safe
        }

        return []; // empty list if type not found
    }

    public List<string> GetDefinitions(string modId)
    {
        if (_modDefinitions.TryGetValue(modId, out var defNames))
        {
            return [.. defNames]; // return a copy to be safe
        }

        return []; // empty list if mod has no definitions
    }

    public bool Exists(string modId, string defName)
    {
        return _modDefinitions.TryGetValue(modId, out var defNames) && defNames.Contains(defName);
    }

    public void SetPayload(string modId, string defName, string propertyName, object? value, string actingModId)
    {
        if (!_modDefinitions.TryGetValue(modId, out var defNames) || !defNames.Contains(defName))
            throw new InvalidOperationException($"[DefinitionManager] Definition '{defName}' does not exist for mod '{modId}'.");

        string fullPath = CreateFullPath([defName, "payload", propertyName]);

        DataManager.Instance.SetData(modId, fullPath, value, actingModId);
    }

    public List<string> GetPayloadPaths(string modId, string defName, string? rootKey = null)
    {
        if (!_defPathsCache.TryGetValue(modId, out var modCache) || !modCache.TryGetValue(defName, out var paths))
            return []; // empty list if mod/def not found

        if (string.IsNullOrEmpty(rootKey))
            return [.. paths]; // return all paths as list

        // Filter paths by rootKey prefix (e.g., "Payload.list")
        return [.. paths.Where(p => p.StartsWith(rootKey + "."))];
    }
}
