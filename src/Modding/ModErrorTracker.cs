using System;
using System.Collections.Generic;

namespace AxiomPlayground.Modding;

public sealed class ModErrorTracker
{
    private static readonly ModErrorTracker _instance = new();
    public static ModErrorTracker Instance => _instance;

    private readonly HashSet<string> _erroredMods = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ModError> _errors = [];

    private ModErrorTracker() { }

    public void MarkModErrored(string modId, string message, string context)
    {
        _erroredMods.Add(modId);
        _errors.Add(new ModError(modId, message, context, DateTime.Now));
        Console.WriteLine($"[ModErrorTracker] Mod '{modId}' disabled due to error in {context}: {message}");
    }

    public bool IsModErrored(string modId)
    {
        return _erroredMods.Contains(modId);
    }

    public IReadOnlyList<ModError> GetErrors() => _errors;

    public IReadOnlySet<string> GetErroredModIds() => _erroredMods;

    public readonly record struct ModError(string ModId, string Message, string Context, DateTime Timestamp);
}
