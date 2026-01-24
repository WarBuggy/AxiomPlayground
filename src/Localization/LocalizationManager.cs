using AxiomPlayground.Data;
using AxiomPlayground.Modding;

namespace AxiomPlayground.Localization;

public sealed class LocalizationManager : BaseManager
{
    private static readonly LocalizationManager _instance = new();
    public static LocalizationManager Instance => _instance;
    private const string DEFAULT_CULTURE = "en-US";
    public static string DefaultCulture => DEFAULT_CULTURE;
    private string _currentCulture = DEFAULT_CULTURE;
    // Logs
    private readonly HashSet<string> _cultureFallbackLogged = [];
    private readonly HashSet<string> _missingKeyLogged = [];

    private LocalizationManager() : base("localization") { }

    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            // We don't need to prepopulate cultures anymore; just check for presence in DataManager
            bool cultureExists = false;

            foreach (var modId in ModManager.Instance.FinalModList.Select(m => m.ModId))
            {
                string path = $"{value}.";
                if (DataManager.Instance.TryGetData(modId, path, out _))
                {
                    cultureExists = true;
                    break;
                }
            }

            if (!cultureExists)
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

    public string GetFrom(string modId, string key, string? culture = null)
    {
        culture ??= _currentCulture;
        string fullPath = CreateFullPath([culture, key]);

        if (DataManager.Instance.TryGetData(modId, fullPath, out var value) && value is string strValue)
            return strValue;

        string logKey = $"{modId}|{culture}|{key}";

        // Fallback to default culture
        if (culture != DEFAULT_CULTURE)
        {
            string fallbackPath = CreateFullPath([DEFAULT_CULTURE, key]);
            if (DataManager.Instance.TryGetData(modId, fallbackPath, out var fallbackValue) && fallbackValue is string fallbackStr)
            {
                if (_cultureFallbackLogged.Add(logKey))
                    Console.WriteLine(
                        $"[LocalizationManager] Missing localization: mod = '{modId}', key = '{key}', culture = '{culture}'. Falling back to '{DEFAULT_CULTURE}'."
                    );

                return fallbackStr;
            }
        }

        if (_missingKeyLogged.Add(logKey))
            Console.WriteLine(
                $"[LocalizationManager] Missing localization key: mod = '{modId}', key= '{key}'. Not found in culture = '{culture}' or default culture = '{DEFAULT_CULTURE}'."
            );

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
