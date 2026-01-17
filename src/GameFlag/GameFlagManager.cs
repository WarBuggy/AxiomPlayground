using System.Reflection;

namespace AxiomPlayground.GameFlag;

public static class GameFlagManager
{
    private static readonly Dictionary<string, GameFlag> _knownFlags =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<GameFlag> _enabledFlags = [];

    static GameFlagManager()
    {
        // Register framework flags first
        RegisterFlagsFromType(typeof(FrameworkGameFlag));

        // Discover and register all other GameFlag subclasses
        var flagTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(GameFlag).IsAssignableFrom(t))
            .Where(t => t != typeof(FrameworkGameFlag)); // skip framework, already registered

        foreach (var type in flagTypes)
        {
            RegisterFlagsFromType(type);
        }
    }

    private static void RegisterFlagsFromType(Type type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                         .Where(f => f.FieldType == type);

        foreach (var field in fields)
        {
            var flag = (GameFlag)field.GetValue(null)!;

            if (_knownFlags.TryGetValue(flag.Name, out var existing))
            {
                LogFlagConflict(flag, existing);
                continue;
            }

            _knownFlags[flag.Name] = flag;
        }
    }

    private static void LogFlagConflict(GameFlag incomingFlag, GameFlag existingFlag)
    {
        Console.WriteLine(
            $"[GameFlag] Flag '{incomingFlag.Name}' conflict: already defined by '{existingFlag.GetType().Name}', incoming '{incomingFlag.GetType().Name}'. Ignored."
        );
    }

    public static void GetFlagsFromArgs(string[] args)
    {
        if (args == null || args.Length == 0)
            return;

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            // Accept -flagname
            var key = arg.TrimStart('-').Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key))
                continue;

            if (_knownFlags.TryGetValue(key, out var flag))
            {
                if (_enabledFlags.Contains(flag))
                {
                    Console.WriteLine(
                        $"[GameFlag] Flag '{key}' is already enabled ({flag.GetType().Name}). Skipping duplicate."
                    );
                    continue;
                }

                // Add to enabled
                _enabledFlags.Add(flag);
                Console.WriteLine(
                    $"[GameFlag] Flag '{key}' ({flag.GetType().Name}) enabled."
                );
            }
            else
            {
                // Unknown flag
                Console.WriteLine(
                    $"[GameFlag] Unknown flag '{key}' in command line arguments. Ignored."
                );
            }
        }
    }

    public static bool IsSet(GameFlag flag) => _enabledFlags.Contains(flag);
}
