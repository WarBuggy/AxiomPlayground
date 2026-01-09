using AxiomPlayground.Data;

namespace AxiomPlayground.Localization;

public sealed class LocalizationManager : BaseManager
{
    private static readonly LocalizationManager _instance = new();
    public static LocalizationManager Instance => _instance;
    private const string DEFAULT_CULTURE = "en-US";
    public static string DefaultCulture => DEFAULT_CULTURE;
    private string _currentCulture = DEFAULT_CULTURE;

    // Updated storage: culture -> modId -> key -> value
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _localizations = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> AvailableCultures => _localizations.Keys;

    // Logs
    private readonly HashSet<string> _cultureFallbackLogged = [];
    private readonly HashSet<string> _missingKeyLogged = [];

    private LocalizationManager() : base("localization") { }

    protected override void ProcessPath(string modId, string path, object? value)
    {
        if (value is not string stringValue)
            return; // Only process string leaves

        int dotIndex = path.IndexOf('.');
        if (dotIndex < 0)
        {
            Console.WriteLine($"[LocalizationManager] Invalid localization path '{path}' in mod '{modId}', skipping.");
            return;
        }

        string culture = path[..dotIndex];
        string key = path[(dotIndex + 1)..];
        AddLocalizationValue(culture, modId, key, stringValue);
    }

    /// <summary>
    /// Adds a single localization entry to the runtime dictionary.
    /// </summary>
    private void AddLocalizationValue(string culture, string modId, string key, string value)
    {
        if (!_localizations.TryGetValue(culture, out var cultureDict))
        {
            cultureDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _localizations[culture] = cultureDict;
        }

        if (!cultureDict.TryGetValue(modId, out var modDict))
        {
            modDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            cultureDict[modId] = modDict;
        }

        modDict[key] = value;
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

    public string GetFromMod(string modId, string key, string? culture = null)
    {
        culture ??= CurrentCulture;

        if (_localizations.TryGetValue(culture, out var cultureDict) &&
            cultureDict.TryGetValue(modId, out var modDict) &&
            modDict.TryGetValue(key, out var value))
        {
            return value;
        }

        string logKey = $"{modId}|{culture}|{key}";

        // Fallback to default culture
        if (culture != DEFAULT_CULTURE &&
            _localizations.TryGetValue(DEFAULT_CULTURE, out var fallbackCultureDict) &&
            fallbackCultureDict.TryGetValue(modId, out var fallbackModDict) &&
            fallbackModDict.TryGetValue(key, out var fallbackValue))
        {
            if (_cultureFallbackLogged.Add(logKey))
                Console.WriteLine($"[LocalizationManager] Missing localization: mod = '{modId}', key = '{key}', culture = '{culture}'. Falling back to '{DEFAULT_CULTURE}'.");

            return fallbackValue;
        }

        if (_missingKeyLogged.Add(logKey))
        {
            Console.WriteLine(
                $"[LocalizationManager] Missing localization key: mod = '{modId}', key= '{key}'. Not found in culture = '{culture}' or default culture = '{DEFAULT_CULTURE}'.");
        }

        return CorruptKey(key);
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
}
