namespace AxiomPlayground.Data;

public class DefinitionManager : BaseManager
{
    private static readonly DefinitionManager _instance = new();
    public static DefinitionManager Instance => _instance;
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _definitions = [];
    private readonly Dictionary<DefinitionKey, HashSet<string>> _definitionPaths = [];

    private DefinitionManager() : base("definition", true) { }

    public override void ProcessPathData(IReadOnlyList<CategoryData> collectedCategoryDataList)
    {
        foreach (var modData in collectedCategoryDataList)
        {
            string modId = modData.ModId;

            if (!_definitions.TryGetValue(modId, out var typeMap))
            {
                typeMap = [];
                _definitions[modId] = typeMap;
            }

            foreach (var pathAndValue in modData.Values)
            {
                var path = pathAndValue.Key;
                var parts = path.Split('.');

                if (parts.Length < 5)
                    continue;

                if (parts[3] != "payload")
                    continue;

                string typeName = parts[1];
                string defName = parts[2];

                if (!typeMap.TryGetValue(typeName, out var defSet))
                {
                    defSet = [];
                    typeMap[typeName] = defSet;
                }
                defSet.Add(defName);

                var key = new DefinitionKey(modId, typeName, defName);
                if (!_definitionPaths.TryGetValue(key, out var paths))
                {
                    paths = [];
                    _definitionPaths[key] = paths;
                }
                paths.Add(path);
            }
        }
    }

    public override IEnumerable<LoadEventDispatch> CollectLoadEvents()
    {
        foreach (var (modId, typeMap) in _definitions)
        {
            foreach (var (typeName, defSet) in typeMap)
            {
                foreach (var defName in defSet)
                {
                    var key = new DefinitionKey(modId, typeName, defName);
                    _definitionPaths.TryGetValue(key, out var defPaths);

                    yield return new LoadEventDispatch(
                        "OnDefinitionCreated",
                        modId,
                        [modId, typeName, defName, (defPaths ?? []).ToArray()]
                    );
                }
            }
        }
    }

    public bool TryGetPayload(string modId, string typeName, string defName, IEnumerable<string> pathParts, out object? value)
    {
        // Prepend typeName and defName to the path
        var fullPathParts = new List<string> { typeName, defName, "payload" };
        fullPathParts.AddRange(pathParts);

        // Build the full path string once
        string fullPath = CreateFullPath([.. fullPathParts]);

        // Try to get the value
        if (DataManager.Instance.TryGetData(modId, fullPath, out value))
            return true;

        value = null;
        return false;
    }

    public void SetPayload(string modId, string typeName, string defName, IEnumerable<string> pathParts, string actingModId, object? value)
    {
        if (!_definitions.TryGetValue(modId, out var typeMap) ||
            !typeMap.TryGetValue(typeName, out var defMap) ||
            !defMap.Contains(defName))
            throw new InvalidOperationException(
                $"[DefinitionManager] Definition '{defName}' of type '{typeName}' does not exist for mod '{modId}'.");

        // Prepend typeName and defName to the path
        var fullPathParts = new List<string> { typeName, defName, "payload" };
        fullPathParts.AddRange(pathParts);

        string fullPath = CreateFullPath([.. fullPathParts]);
        DataManager.Instance.SetData(modId, fullPath, actingModId, value);
    }

    public override void CleanupAfterLoadEvents()
    {
        foreach (var paths in _definitionPaths.Values)
            paths.Clear();
    }

    private readonly record struct DefinitionKey
    (
        string ModId,
        string TypeName,
        string DefName
    );
}
