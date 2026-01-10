namespace AxiomPlayground.Localization;

public abstract class LoggerBase
{
    private string ClassName => GetType().Name;

    protected readonly string ModId;

    // Constructor allows specifying the mod context
    protected LoggerBase(string modId)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentNullException(nameof(modId), "[LoggerBase] modId cannot be null or empty.");
        ModId = modId;
    }

    public void Log(string key, params object[] args)
    {
        Console.WriteLine($"[{ClassName}] Log: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogWarning(string key, params object[] args)
    {
        Console.WriteLine($"[{ClassName}] Warning: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogError(string key, params object[] args)
    {
        Console.WriteLine($"[{ClassName}] Error: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogErrorWithEnding(string ending, string key, params object[] args)
    {
        Console.WriteLine($"[{ClassName}] Error: {StringUtils.LocalizeWithEndingFrom(ModId, ending, key, args)}");
    }
}