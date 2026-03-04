namespace AxiomPlayground.Data;

public sealed class PathHistory
{
    private readonly List<PathEvent> _events = [];

    public PathHistory() { }

    public PathHistory(PathHistory other)
    {
        _events = [.. other._events];
    }

    public void AddCreate(string modId, object? value)
    {
        _events.Add(new PathEvent(modId, PathEventType.Create, value));
    }

    public void AddOverwrite(string modId, object? value)
    {
        _events.Add(new PathEvent(modId, PathEventType.Overwrite, value));
    }

    public void AddDeleted(string modId, object? value, IReadOnlyList<string> causedByPaths)
    {
        if (causedByPaths == null || causedByPaths.Count == 0)
            throw new ArgumentException("[PathHistory] causedByPaths must contain at least one path.", nameof(causedByPaths));

        _events.Add(new PathEvent(modId, PathEventType.Deleted, value, causedByPaths));
    }

    public void Print(string path)
    {
        if (_events.Count == 0)
        {
            Console.WriteLine($"No history for path '{path}'.");
            return;
        }

        Console.WriteLine($"History for path '{path}':");

        for (int i = 0; i < _events.Count; i++)
        {
            var evt = _events[i];
            string causedByPart = (evt.CausedByPaths != null && evt.CausedByPaths.Count > 0)
                ? $" (caused by '{string.Join(", ", evt.CausedByPaths)}')"
                : " (caused by '<N/A>')";

            string valuePart = evt.Value != null ? $" | Value: {evt.Value}" : " | Value: <null>";

            Console.WriteLine($"  {i + 1}. {evt.Type} by {evt.ActingModId}{valuePart}{causedByPart}");
        }
    }
}

public sealed class PathEvent(string actingModId, PathEventType type, object? value, IReadOnlyList<string>? causedByPaths = null)
{
    public string ActingModId { get; } = actingModId;// acting mod
    public PathEventType Type { get; } = type; // Create | Overwrite | Derived
    public object? Value { get; } = value; // value at this point
    public IReadOnlyList<string> CausedByPaths { get; } = causedByPaths ?? []; // for Derived only
}

public enum PathEventType
{
    Create,
    Overwrite,
    Deleted,
}
