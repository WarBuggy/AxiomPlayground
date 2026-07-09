namespace AxiomPlayground.Shared;

public class BootstrapException : Exception
{
    public string LocalizationKey { get; }

    public object[] Arguments { get; }

    public BootstrapException(
        string localizationKey,
        params object[] arguments)
        : base(localizationKey)
    {
        LocalizationKey = localizationKey;
        Arguments = arguments;
    }

    public BootstrapException(
        string localizationKey,
        Exception innerException,
        params object[] arguments)
        : base(localizationKey, innerException)
    {
        LocalizationKey = localizationKey;
        Arguments = arguments;
    }
}