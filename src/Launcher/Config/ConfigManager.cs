namespace Launcher.Config;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class ConfigManager
{
    public static LauncherConfig Launcher { get; private set; } = new();

    private static readonly string Path =
        System.IO.Path.Combine(AppContext.BaseDirectory, "..", "launcherConfig.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Load()
    {
        var configTypes = DiscoverConfigs();

        JsonObject root;

        // Load existing file or create empty root
        if (!File.Exists(Path))
        {
            root = [];
        }
        else
        {
            root = JsonNode.Parse(File.ReadAllText(Path))?.AsObject() ?? [];
        }

        foreach (var type in configTypes)
        {
            var defaultInstance = (BaseConfig)Activator.CreateInstance(type)!;
            var sectionName = defaultInstance.GetSectionName();

            JsonObject defaultJson = JsonSerializer.SerializeToNode(defaultInstance, JsonOptions)!.AsObject();

            JsonObject finalSection;

            if (!root.ContainsKey(sectionName))
            {
                // No existing config, use defaults
                finalSection = defaultJson;
            }
            else
            {
                // Load existing config as raw JSON DOM
                var existingNode = root[sectionName]!.AsObject();

                // Merge missing fields from defaults (same JSON shape assumed)
                MergeMissing(existingNode, defaultJson);

                finalSection = existingNode;
            }

            root[sectionName] = finalSection;

            // Store runtime strongly typed config
            StoreRuntime(type, finalSection);
        }

        // Save updated + normalized config file
        File.WriteAllText(Path, root.ToJsonString(JsonOptions));
    }

    private static List<Type> DiscoverConfigs()
    {
        return [.. Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                typeof(BaseConfig).IsAssignableFrom(t))];
    }

    private static void MergeMissing(JsonObject target, JsonObject defaults)
    {
        foreach (var kv in defaults)
        {
            if (!target.ContainsKey(kv.Key))
                //target[kv.Key] = kv.Value;
                target[kv.Key] = kv.Value!.DeepClone();
        }
    }

    private static void StoreRuntime(Type type, JsonObject json)
    {
        if (type == typeof(LauncherConfig))
        {
            Launcher = json.Deserialize<LauncherConfig>(JsonOptions) ?? new LauncherConfig();
        }
    }
}