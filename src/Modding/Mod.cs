namespace AxiomPlayground.Modding;

public enum ModSource
{
    Steam,
    Local
}

public sealed class Mod(string modId, ModSource source)
{
    public string ModId { get; set; } = modId;
    public string DisplayName { get; set; } = modId;
    public ModSource Source { get; set; } = source;
    public bool Enabled { get; set; } = true;
    private readonly Dictionary<string, object> _runtimeData = new(StringComparer.OrdinalIgnoreCase);

    public void SetRuntimeData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("[Mod] Runtime data key cannot be null or empty.", nameof(key));
        _runtimeData[key] = value;
    }

    public bool TryGetRuntimeData(string key, out object value)
    {
        return _runtimeData.TryGetValue(key, out value!);
    }

    public object? GetRuntimeData(string key)
    {
        return _runtimeData.TryGetValue(key, out var value) ? value : null;
    }

    public void ClearRuntimeData()
    {
        _runtimeData.Clear();
    }

    public bool RemoveRuntimeData(string key)
    {
        return _runtimeData.Remove(key);
    }
}
