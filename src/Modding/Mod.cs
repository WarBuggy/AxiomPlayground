using AxiomPlayground.Modding.Metadata.Model;
using AxiomPlayground.Shared;

namespace AxiomPlayground.Modding;

public sealed class Mod(ModInfo info, ModSource source, bool frameworkDebugEnabled = false)
{
    public ModInfo Info { get; } = info ?? throw new ArgumentNullException(nameof(info));
    public string DisplayLabel { get; set; } = "";
    public ModSource Source { get; } = source;
    public bool Enabled { get; set; } = true;
    private readonly Dictionary<string, object> _runtimeData = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _frameworkDebugEnabled = frameworkDebugEnabled;
    private readonly Dictionary<string, List<RuntimeEvent>> _runtimeHistory = [];

    public void SetRuntimeData(string actingModId, string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("[Mod] Runtime data key cannot be null or empty.", nameof(key));

        bool existed = _runtimeData.ContainsKey(key);
        _runtimeData[key] = value;

        RecordRuntimeEvent(
            actingModId,
            key,
            existed ? RuntimeEventType.Overwrite : RuntimeEventType.Create,
            value
        );
    }

    public bool TryGetRuntimeData(string key, out object value)
    {
        return _runtimeData.TryGetValue(key, out value!);
    }

    public object? GetRuntimeData(string key)
    {
        return _runtimeData.TryGetValue(key, out var value) ? value : null;
    }

    public void ClearRuntimeData(string actingModId)
    {
        if (_runtimeData.Count == 0)
            return;

        if (_frameworkDebugEnabled)
        {
            foreach (var kv in _runtimeData)
            {
                RecordRuntimeEvent(
                    actingModId,
                    kv.Key,
                    RuntimeEventType.ClearAll,
                    kv.Value
                );
            }
        }

        _runtimeData.Clear();
    }

    public string GetDisplayName(bool isInDuplicateGroup)
    {
        if (isInDuplicateGroup)
            return $"({Source}) {Info.Name}";

        return Info.Name;
    }

    public bool RemoveRuntimeData(string actingModId, string key)
    {
        if (!_runtimeData.TryGetValue(key, out var existing))
            return false;

        _runtimeData.Remove(key);

        RecordRuntimeEvent(
            actingModId,
            key,
            RuntimeEventType.Remove,
            existing
        );

        return true;
    }

    public override string ToString()
    {
        return $"{Info.Id} | {Info.Name} ({Source})";
    }

    #region FrameworkGameFlag.Debug

    private void RecordRuntimeEvent
    (
        string actingModId,
        string key,
        RuntimeEventType type,
        object? value = null
    )
    {
        if (!_frameworkDebugEnabled)
            return;

        if (!_runtimeHistory.TryGetValue(key, out var list))
        {
            list = [];
            _runtimeHistory[key] = list;
        }

        list.Add(new RuntimeEvent
        {
            Type = type,
            ActingModId = actingModId,
            Value = value,
        });
    }

    private enum RuntimeEventType
    {
        Create,
        Overwrite,
        Remove,
        ClearAll
    }

    private sealed class RuntimeEvent
    {
        public RuntimeEventType Type { get; init; }
        public string ActingModId { get; init; } = "";
        public object? Value { get; init; }
    }

    public IReadOnlyList<(string ModId, string Event, object? Value)> GetRuntimeHistory(string key)
    {
        if (!_frameworkDebugEnabled)
        {
            Console.WriteLine
            (
                "[Mod] Framework debug mode is not enabled. " +
                "Runtime data history is not recorded. " +
                "Start the game with the '-debug' argument to enable runtime history tracking."
            );
            return [];
        }

        if (!_runtimeHistory.TryGetValue(key, out var list))
        {
            Console.WriteLine
            (
                $"[Mod] No runtime history found for key '{key}'. " +
                "The key has never been created or modified by any mod."
            );
            return [];
        }

        return list.Select(e => (e.ActingModId, e.Type.ToString(), e.Value)).ToList();
    }

    public List<string> GetAllRuntimeHistoryKeys()
    {
        if (!_frameworkDebugEnabled)
        {
            Console.WriteLine
            (
                "[Mod] Framework debug mode is not enabled. " +
                "Function GetAllRuntimeHistoryKeys is disabled. " +
                "Start the game with the '-debug' argument to enable full data path history."
            );
            return [];
        }
        return [.. _runtimeHistory.Keys];
    }

    #endregion
}
