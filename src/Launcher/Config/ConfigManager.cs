
using AxiomPlayground.Shared;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Launcher.Config;

public static class ConfigManager
{
    private static JsonObject _root = [];

    public static LauncherConfig Launcher { get; private set; } = new();

    private static readonly string Path =
        System.IO.Path.Combine(AppContext.BaseDirectory, "launcherConfig.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Load()
    {
        var configTypes = DiscoverConfigs();

        // Load existing file or create empty root
        if (!File.Exists(Path))
        {
            _root = [];
        }
        else
        {
            _root = JsonNode.Parse(File.ReadAllText(Path))?.AsObject() ?? [];
        }

        foreach (var type in configTypes)
        {
            var defaultInstance = (BaseConfig)Activator.CreateInstance(type)!;
            var sectionName = defaultInstance.GetSectionName();

            JsonObject defaultJson = JsonSerializer.SerializeToNode(defaultInstance, type, JsonOptions)!.AsObject();

            JsonObject finalSection;

            if (!_root.ContainsKey(sectionName))
            {
                // No existing config, use defaults
                finalSection = defaultJson;
            }
            else
            {
                // Load existing config as raw JSON DOM
                var existingNode = _root[sectionName]!.AsObject();

                // Merge missing fields from defaults (same JSON shape assumed)
                MergeMissing(existingNode, defaultJson);

                finalSection = existingNode;
            }

            _root[sectionName] = finalSection;

            // Store runtime strongly typed config
            StoreRuntime(type, finalSection);
        }

        // Save updated + normalized config file
        File.WriteAllText(Path, _root.ToJsonString(JsonOptions));
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

    public static void Set<TConfig, TValue>(string propertyName, TValue value)
    where TConfig : BaseConfig
    {
        var type = typeof(TConfig);
        var instance = (BaseConfig)Activator.CreateInstance(type)!;
        var section = instance.GetSectionName();

        if (!_root.ContainsKey(section))
            throw new Exception(Shared.T("errorConfigManagerSectionNotLoaded", section));

        var sectionObj = _root[section]!.AsObject();

        var jsonKey = JsonNamingPolicy.CamelCase.ConvertName(propertyName);

        sectionObj[jsonKey] =
            JsonSerializer.SerializeToNode(value, JsonOptions)!.DeepClone();

        Persist();
        RefreshRuntime<TConfig>();
    }

    private static void Persist()
    {
        var json = _root.ToJsonString(JsonOptions);

        var temp = Path + ".tmp";

        File.WriteAllText(temp, json);

        if (File.Exists(Path))
            File.Delete(Path);

        File.Move(temp, Path);
    }

    private static void RefreshRuntime<TConfig>() where TConfig : BaseConfig
    {
        var type = typeof(TConfig);
        var section = ((BaseConfig)Activator.CreateInstance(type)!).GetSectionName();

        var json = _root[section]!.ToJsonString();

        if (type == typeof(LauncherConfig))
        {
            Launcher =
                JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions)
                ?? new LauncherConfig();
        }
    }
}