using MoonSharp.Interpreter;

namespace AxiomPlayground.Data;

public class DefinitionManager : BaseManager
{
    private static readonly DefinitionManager _instance = new();
    public static DefinitionManager Instance => _instance;
    private readonly Dictionary<string, HashSet<string>> _modDefinitions = [];
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _definitionTypes = [];

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

            foreach (var pathAndValue in definitions)
            {
                var path = pathAndValue.Key;
                var pathParts = path.Split(".");

                if (pathParts.Length < 3)
                    continue;

                string defName = pathParts[1];

                if (_modDefinitions[modId].Contains(defName))
                    continue;

                _modDefinitions[modId].Add(defName);

                string typePath = $"definition.{defName}.Type";
                if (!definitions.TryGetValue(typePath, out var typeValue))
                    continue;

                string? typeStr = typeValue as string;
                if (string.IsNullOrEmpty(typeStr))
                    continue;

                if (!_definitionTypes[modId].TryGetValue(typeStr, out var defList))
                {
                    defList = [];
                    _definitionTypes[modId][typeStr] = defList;
                }
                defList.Add(defName);
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

    public bool TryGet(string modId, string defName, string propertyName, out object? value)
    {
        value = null;

        if (!_modDefinitions.TryGetValue(modId, out var defNames) || !defNames.Contains(defName))
            return false;

        // Build full path
        string fullPath = CreateFullPath([defName, "Data", propertyName]);

        if (DataManager.Instance.TryGetData(modId, fullPath, out value))
        {
            return true;
        }

        return false; // property does not exist
    }

    public bool TryGetType(string modId, string defName, out string? type)
    {
        type = null;

        // Check if the mod has definitions
        if (!_modDefinitions.TryGetValue(modId, out var defNames) || !defNames.Contains(defName))
            return false;

        // Look for the type mapping
        if (_definitionTypes.TryGetValue(modId, out var typeDict))
        {
            foreach (var kv in typeDict)
            {
                if (kv.Value.Contains(defName))
                {
                    type = kv.Key;
                    return true;
                }
            }
        }

        return false; // definition exists but has no type
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

    public void Set(string modId, string defName, string propertyName, object? value, string actingModId)
    {
        if (!_modDefinitions.TryGetValue(modId, out var defNames) || !defNames.Contains(defName))
            throw new InvalidOperationException($"[DefinitionManager] Definition '{defName}' does not exist for mod '{modId}'.");

        string fullPath = CreateFullPath(new[] { defName, "Data", propertyName });

        DataManager.Instance.SetData(modId, fullPath, value, actingModId);
    }
}
