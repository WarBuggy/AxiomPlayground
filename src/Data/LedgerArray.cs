namespace AxiomPlayground.Data;

public sealed class LedgerArray : LedgerMap
{
    private static void ValidateKey(int index)
    {
        if (index < 1)
            throw new ArgumentException("[LedgerArray] index must be >= 1", nameof(index));
    }

    private static string IndexKey(int index) => index.ToString();

    // Override TryGet
    public bool TryGet(int index, out object value)
    {
        ValidateKey(index);
        return base.TryGet(IndexKey(index), out value!);
    }

    public bool TryGet(int index, out object? value, out string ownerId)
    {
        ValidateKey(index);
        return base.TryGet(IndexKey(index), out value, out ownerId);
    }

    // Override Set
    public void Set(int index, object value, string actorId)
    {
        ValidateKey(index);
        base.Set(IndexKey(index), value, actorId);
    }

    // Override TryRemove
    public bool TryRemove(int index, string actorId)
    {
        ValidateKey(index);

        string key = IndexKey(index);
        if (!TryGet(key, out _))
            return false;

        // Remove the value at index
        TryRemove(key, actorId);

        // Shift subsequent indices down
        ShiftIndicesFrom(index + 1, -1, actorId);

        return true;
    }

    public int InsertAt(int index, object value, string actorId)
    {
        ValidateKey(index);

        ShiftIndicesFrom(index, 1, actorId);

        Set(IndexKey(index), value, actorId);

        return index;
    }
    public int InsertFirst(object value, string actorId) => InsertAt(1, value, actorId);

    public int InsertLast(object value, string actorId)
    {
        int nextIndex = _values.Count + 1;
        return InsertAt(nextIndex, value, actorId);
    }

    public bool TryInsertBeforeValue(object existingValue, object newValue, string actorId, out int newIndex)
    {
        newIndex = -1;
        string? key = GetKeyOf(existingValue);
        if (key == null || !int.TryParse(key, out int existingIndex))
            return false;

        newIndex = existingIndex;
        InsertAt(newIndex, newValue, actorId);
        return true;
    }

    public bool TryInsertAfterValue(object existingValue, object newValue, string actorId, out int newIndex)
    {
        newIndex = -1;
        string? key = GetKeyOf(existingValue);
        if (key == null || !int.TryParse(key, out int existingIndex))
            return false;

        newIndex = existingIndex + 1;
        InsertAt(newIndex, newValue, actorId);
        return true;
    }

    public int GetIndexOf(object value)
    {
        string? key = GetKeyOf(value);
        if (key == null)
            return -1;

        if (!int.TryParse(key, out var index))
        {
            return -1;
        }
        return index;
    }

    private void ShiftIndicesFrom(int startIndex, int delta, string actorId)
    {
        // Find all numeric keys >= startIndex
        var keysToShift = _values.Keys
            .Where(k => int.TryParse(k, out int i) && i >= startIndex)
            .Select(k => int.Parse(k))
            .OrderByDescending(i => i) // shift from back to front
            .ToList();

        foreach (var oldIndex in keysToShift)
        {
            string oldKey = IndexKey(oldIndex);
            string newKey = IndexKey(oldIndex + delta);

            if (TryGet(oldKey, out var val))
            {
                TryRemove(oldKey, actorId);
                Set(newKey, val, actorId);
            }
        }
    }
}