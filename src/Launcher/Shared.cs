namespace Launcher;

using System.Globalization;
using System.Resources;

public static class Shared
{
    private static readonly ResourceManager Loc =
        new("Launcher.Localization", typeof(Shared).Assembly);

    public static string T(string key)
    {
        var value = Loc.GetString(key, CultureInfo.CurrentUICulture);
        return value ?? key;
    }

    public static string T(string key, params object[] args)
    {
        var format = Loc.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        return string.Format(format, args);
    }
}