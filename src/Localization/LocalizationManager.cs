using System.Text.Json;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Localization;

public sealed class LocalizationManager
{
    private static readonly LocalizationManager _instance = new();
    public static LocalizationManager Instance => _instance;
    private const string LOCALIZATION_FOLDER = "Localization";
    private const string DEFAULT_CULTURE = "en-US";
    public static string DefaultCulture => DEFAULT_CULTURE;
    private string _currentCulture = DEFAULT_CULTURE;

    // Updated storage: culture -> modId -> key -> value
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _localizations = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> AvailableCultures => _localizations.Keys;

    // Logs when a mod falls back from a non-default culture to default culture
    private readonly HashSet<string> _cultureFallbackLogged = new();
    // Logs when a mod is missing a key even in default culture
    private readonly HashSet<string> _missingKeyLogged = new();

    // Tracks cross-mod injection logging (once per file)
    private readonly HashSet<string> _loggedFiles = [];
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, };

    private LocalizationManager() { }

    /// <summary>
    /// Load all localization files for the provided mods.
    /// </summary>
    public void LoadAll(List<ModInstance> mods)
    {
        if (mods == null || mods.Count == 0)
            throw new InvalidOperationException("[LocalizationManager] No mods provided. Cannot load localization data.");

        foreach (var mod in mods)
        {
            string modRoot = ModManager.Instance.GetModFolderPath(mod);
            string localizationRoot = Path.Combine(modRoot, LOCALIZATION_FOLDER);

            if (!Directory.Exists(localizationRoot))
                continue; // Mods can have no localization

            foreach (string cultureDir in Directory.GetDirectories(localizationRoot))
            {
                string cultureName = Path.GetFileName(cultureDir);
                LoadCulture(cultureName, cultureDir, mod.ModId);
            }
        }

        if (!_localizations.ContainsKey(DEFAULT_CULTURE))
        {
            throw new InvalidOperationException(
                $"[LocalizationManager] Default culture '{DEFAULT_CULTURE}' is missing. Game cannot start."
            );
        }

        Console.WriteLine($"[LocalizationManager] Loaded {_localizations.Count} cultures.");
    }

    private void LoadCulture(string culture, string culturePath, string owningModId)
    {
        Console.WriteLine($"[LocalizationManager] Loading culture: {culture}.");

        if (!_localizations.TryGetValue(culture, out var cultureDict))
        {
            cultureDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _localizations[culture] = cultureDict;
        }

        foreach (string file in Directory.GetFiles(culturePath, "*.json", SearchOption.AllDirectories))
        {
            LoadLocalizationFile(cultureDict, file, owningModId);
        }
    }

    private void LoadLocalizationFile(Dictionary<string, Dictionary<string, string>> cultureDict, string filePath, string owningModId)
    {
        LocalizationFileModel? model;
        try
        {
            string jsonText = File.ReadAllText(filePath);
            model = JsonSerializer.Deserialize<LocalizationFileModel>(jsonText, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalizationManager] Warning: Failed to parse localization file '{filePath}': {ex.Message}");
            return;
        }

        if (model?.Data == null || model.Data.Count == 0)
        {
            Console.WriteLine($"[LocalizationManager] Warning: Localization file '{filePath}' has no data, skipping.");
            return;
        }

        // Determine target mod
        string targetModId = ResolveTargetModId(model, owningModId, filePath);

        // Log cross-mod injection once per file
        if (targetModId != owningModId)
            LogInfoOnce($"[LocalizationManager] Mod '{owningModId}' {(model.Overwrite != null ? "overwrite" : "addTo")} '{targetModId}' (file: {Path.GetFileName(filePath)}).");

        // Apply localization data
        ApplyLocalizationData(cultureDict, targetModId, model.Data, model.Overwrite != null);
    }

    private static string ResolveTargetModId(LocalizationFileModel model, string owningModId, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(model.Overwrite))
        {
            if (!string.IsNullOrWhiteSpace(model.AddTo))
            {
                Console.WriteLine($"[LocalizationManager] Warning: File '{filePath}' defines both addTo and overwrite. Using overwrite.");
            }
            return model.Overwrite;
        }

        if (!string.IsNullOrWhiteSpace(model.AddTo))
            return model.AddTo;

        return owningModId; // Default: add to current mod
    }

    private static void ApplyLocalizationData(
        Dictionary<string, Dictionary<string, string>> cultureDict,
        string targetModId,
        Dictionary<string, string> data,
        bool overwrite)
    {
        if (!cultureDict.TryGetValue(targetModId, out var modDict))
        {
            modDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            cultureDict[targetModId] = modDict;
        }

        foreach (var (key, value) in data)
        {
            if (overwrite)
                modDict[key] = value;
            else if (!modDict.ContainsKey(key))
                modDict[key] = value;
        }
    }

    private void LogInfoOnce(string message)
    {
        if (_loggedFiles.Add(message))
            Console.WriteLine(message);
    }

    /// <summary>
    /// Gets a localized string only from a specific mod.
    /// Falls back to the default culture for the same mod if not found.
    /// Returns a visibly corrupted key if the string does not exist.
    /// </summary>
    /// <param name="modId">The mod to look in.</param>
    /// <param name="key">The localization key.</param>
    /// <param name="culture">Optional culture. Defaults to current culture.</param>
    /// <returns>Localized string from the specified mod.</returns>
    public string GetFromMod(string modId, string key, string? culture = null)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentNullException(nameof(modId), "[LocalizationManager] modId cannot be null or empty.");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "[LocalizationManager] key cannot be null or empty.");

        culture ??= CurrentCulture;

        if (_localizations.TryGetValue(culture, out var cultureDict) &&
            cultureDict.TryGetValue(modId, out var modDict) &&
            modDict.TryGetValue(key, out var value))
        {
            return value;
        }

        string logKey = $"{modId}|{culture}|{key}";

        if (culture != DEFAULT_CULTURE &&
            _localizations.TryGetValue(DEFAULT_CULTURE, out var fallbackCultureDict) &&
            fallbackCultureDict.TryGetValue(modId, out var fallbackModDict) &&
            fallbackModDict.TryGetValue(key, out var fallbackValue))
        {
            // Log once per (modId, culture, key)
            if (_cultureFallbackLogged.Add(logKey))
            {
                Console.WriteLine($"[LocalizationManager] Missing localization: mod = '{modId}', key = '{key}', culture = '{culture}'. Falling back to '{DEFAULT_CULTURE}'.");
            }

            return fallbackValue;
        }

        if (_missingKeyLogged.Add(logKey))
        {
            Console.WriteLine(
                $"[LocalizationManager] Missing localization key: mod = '{modId}', key= '{key}'. Not found in culture = '{culture}' or default culture = '{DEFAULT_CULTURE}'.");
        }

        return CorruptKey(key);
    }


    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (!_localizations.ContainsKey(value))
            {
                Console.WriteLine(
                    $"[LocalizationManager] Warning: Attempted to set unknown culture '{value}'. Falling back to default '{DEFAULT_CULTURE}'."
                );
                _currentCulture = DEFAULT_CULTURE;
            }
            else
            {
                _currentCulture = value;
            }
        }
    }

    private static string CorruptKey(string key)
    {
        var map = new Dictionary<char, char>
        {
            ['a'] = 'à',
            ['A'] = 'Á',
            ['e'] = 'è',
            ['E'] = 'É',
            ['i'] = 'ì',
            ['I'] = 'Í',
            ['o'] = 'ò',
            ['O'] = 'Ó',
            ['u'] = 'ù',
            ['U'] = 'Ú',
            ['r'] = 'ř',
            ['R'] = 'Ř',
            ['s'] = 'š',
            ['S'] = 'Š',
        };

        var chars = key.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (map.TryGetValue(chars[i], out char replacement))
                chars[i] = replacement;
        }
        return new string(chars);
    }

    private sealed class LocalizationFileModel
    {
        public string? AddTo { get; set; }
        public string? Overwrite { get; set; }
        public Dictionary<string, string>? Data { get; set; }
    }
}
