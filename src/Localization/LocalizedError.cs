using System;
using System.Diagnostics;

namespace AxiomPlayground.Localization;

public abstract class LocalizedError<TException> : Exception where TException : Exception
{
    private readonly string _modId;

    /// <summary>
    /// Protected constructor requiring a modId.
    /// </summary>
    /// <param name="modId">The mod id for localization</param>
    protected LocalizedError(string modId)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentNullException(nameof(modId), "[LocalizedError] modId cannot be null or empty.");
        _modId = modId;
    }

    /// <summary>
    /// Throws a localized error with default ending.
    /// </summary>
    /// <param name="key">Localization key</param>
    /// <param name="args">Format arguments</param>
    protected void Throw(string key, params object[] args)
    {
        ThrowLocalized(".", key, args);
    }

    /// <summary>
    /// Throws a localized error with custom ending.
    /// </summary>
    /// <param name="endingWrapper">Custom ending</param>
    /// <param name="key">Localization key</param>
    /// <param name="args">Format arguments</param>
    protected void Throw(EndingWrapper endingWrapper, string key, params object[] args)
    {
        ThrowLocalized(endingWrapper.Value, key, args);
    }

    private void ThrowLocalized(string ending, string key, object[] args)
    {
        string className = ResolveCallerClassName();
        string message = $"[{className}] {StringUtils.LocalizeWithEndingFrom(_modId, ending, key, args)}";

        Exception? innerException = null;
        var ctor = typeof(TException).GetConstructor([typeof(string), typeof(Exception)])
                   ?? throw new InvalidOperationException(
                        $"[LocalizedError] Exception type {typeof(TException).Name} must define a constructor (string, Exception)."
                   );

        var exception = (TException)ctor.Invoke([message, innerException]);
        throw exception;
    }

    private static string ResolveCallerClassName()
    {
        var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: false);

        foreach (var frame in stackTrace.GetFrames()!)
        {
            var type = frame.GetMethod()?.DeclaringType;
            if (type != null && !type.IsGenericType)
                return type.Name;
        }

        return "UnknownClass";
    }
}

/// <summary>
/// Wrapper for optional custom ending.
/// </summary>
public readonly struct EndingWrapper(string value)
{
    public string Value { get; } = value ?? "";
}
