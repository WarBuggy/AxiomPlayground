using System.Text;

namespace AxiomPlayground.Localization;

public static class StringUtils
{
    /// <summary>
    /// Localizes a string from a specific mod only with a custom ending.
    /// Looks in the current culture.
    /// </summary>
    public static string LocalizeWithEndingFrom(string modId, string ending, string key, params object[] formatArgs)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentNullException(nameof(modId), "[AxiomPlayground.StringUtils] modId cannot be null or empty.");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "[AxiomPlayground.StringUtils] key cannot be null or empty.");

        ending ??= "";

        string message = LocalizationManager.Instance.GetFromMod(modId, key);
        // Parse the message to detect how many arguments it expects
        var compositeFormat = CompositeFormat.Parse(message);
        var requiredArgs = compositeFormat.MinimumArgumentCount;
        // Pad missing arguments with empty strings
        if (formatArgs.Length < requiredArgs)
        {
            var paddedArgs = new object[requiredArgs];
            Array.Copy(formatArgs, paddedArgs, formatArgs.Length);
            for (int i = formatArgs.Length; i < requiredArgs; i++)
                paddedArgs[i] = "";
            formatArgs = paddedArgs;
        }
        string formatted = formatArgs.Length > 0 ? string.Format(message, formatArgs) : message;
        return formatted + ending;
    }
}
