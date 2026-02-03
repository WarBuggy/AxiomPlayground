using AxiomPlayground.GameFlag;

namespace AxiomPlayground.Data;

public class LedgerMap
{
    protected readonly Dictionary<string, MapItem> _values = [];
    private readonly List<MapEvent> _ledger = [];
    private readonly bool _trackingEnabled =
        GameFlagManager.IsSet(FrameworkGameFlag.Debug);
    public IReadOnlyList<MapEvent> Ledger => _ledger.AsReadOnly();
    public int Count => _values.Count;
    public IEnumerable<string> Keys => _values.Keys;

    public bool TryGet(string key, out object? value)
    {
        ValidateKey(key);

        if (_values.TryGetValue(key, out var item))
        {
            value = item.Value;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGet(string key, out object? value, out string ownerId)
    {
        ValidateKey(key);

        if (_values.TryGetValue(key, out var item))
        {
            value = item.Value;
            ownerId = item.OwnerId;
            return true;
        }

        value = null;
        ownerId = string.Empty;
        return false;
    }

    public bool ContainsKey(string key)
    {
        ValidateKey(key);
        return _values.ContainsKey(key);
    }

    public void Set(string key, object? value, string actorId)
    {
        ValidateKey(key);
        ValidateActor(actorId);

        bool existed = _values.TryGetValue(key, out var oldItem);

        MapItem newItem;
        if (existed)
            newItem = oldItem! with { Value = value };
        else
            newItem = new MapItem(key, actorId, value);

        var evt = new SetEvent(newItem, actorId);
        Apply(evt);
        Append(evt);
    }

    public bool TryRemove(string key, string actorId)
    {
        ValidateKey(key);
        ValidateActor(actorId);

        if (!_values.TryGetValue(key, out var oldItem))
            return false;

        var evt = new RemoveEvent(oldItem, actorId);
        Apply(evt);
        Append(evt);

        return true;
    }

    public void Clear(string actorId)
    {
        ValidateActor(actorId);

        if (_values.Count == 0)
            return;

        var evt = new ClearEvent(ActorId: actorId);

        Apply(evt);
        Append(evt);
    }

    public string? GetKeyOf(object value)
    {
        foreach (var kvp in _values)
        {
            if (Equals(kvp.Value.Value, value))
                return kvp.Key;
        }
        return null;
    }

    private void Append(MapEvent evt)
    {
        if (_trackingEnabled)
            _ledger.Add(evt);
    }

    private void Apply(MapEvent evt)
    {
        switch (evt)
        {
            case SetEvent set:
                _values[set.Item.Key] = set.Item;
                break;

            case RemoveEvent remove:
                _values.Remove(remove.Item.Key);
                break;

            case ClearEvent:
                _values.Clear();
                break;

            default:
                throw new InvalidOperationException(
                    $"[LedgerMap] Unknown event type {evt.GetType().Name}"
                );
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "[LedgerMap] key must be a non-empty string",
                nameof(key)
            );
    }

    private static void ValidateActor(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            throw new ArgumentException(
                "[LedgerMap] actorId must be non-empty",
                nameof(actorId)
            );
    }

    public abstract record MapEvent(string ActorId);

    protected sealed record SetEvent(MapItem Item, string ActorId) : MapEvent(ActorId);

    protected sealed record RemoveEvent(MapItem Item, string ActorId) : MapEvent(ActorId);

    protected sealed record ClearEvent(string ActorId) : MapEvent(ActorId);

    protected sealed record MapItem(string Key, string OwnerId, object? Value);
}
