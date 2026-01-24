using System.Diagnostics;

namespace AxiomPlayground.Localization;

public abstract class LoggerBase
{
    protected readonly string ModId;

    protected LoggerBase(string modId)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentNullException(nameof(modId), "[LoggerBase] modId cannot be null or empty.");
        ModId = modId;
    }

    private static string GetCallerClassName()
    {
        var stackTrace = new StackTrace();

        for (int i = 2; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame?.GetMethod(); // frame might be null
            var declaringType = method?.DeclaringType;
            if (declaringType != null && declaringType != typeof(LoggerBase))
            {
                return declaringType.Name;
            }
        }

        return "UnknownClass";
    }

    public void Log(string key, params object[] args)
    {
        string className = GetCallerClassName();
        Console.WriteLine($"[{className}] Log: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogWarning(string key, params object[] args)
    {
        string className = GetCallerClassName();
        Console.WriteLine($"[{className}] Warning: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogError(string key, params object[] args)
    {
        string className = GetCallerClassName();
        Console.WriteLine($"[{className}] Error: {StringUtils.LocalizeWithEndingFrom(ModId, ".", key, args)}");
    }

    public void LogErrorWithEnding(string ending, string key, params object[] args)
    {
        string className = GetCallerClassName();
        Console.WriteLine($"[{className}] Error: {StringUtils.LocalizeWithEndingFrom(ModId, ending, key, args)}");
    }
}
