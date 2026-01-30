using System.Collections.ObjectModel;
using AxiomPlayground.GameFlag;

namespace AxiomPlayground.Data
{
    public sealed class LedgerArray<T>
    {
        private int _nextItemId = 1;
        private int _nextEventId = 1;
        private readonly List<ArrayEvent> _ledger = [];
        private readonly List<ArrayItem> _items = [];
        private readonly Dictionary<int, int> _indexByItemId = [];
        public IEnumerable<T> Values => _items.Select(i => i.Value);
        public IReadOnlyList<ArrayEvent> Ledger =>
            _ledger.AsReadOnly();
        public IReadOnlyList<ItemOwnership> CurrentOwnership =>
            new ReadOnlyCollection<ItemOwnership>(
                [.. _items.Select(i => new ItemOwnership(i.ItemId, i.OwnerId))]
            );
        private readonly bool _trackingEnabled = GameFlagManager.IsSet(FrameworkGameFlag.Debug);
        public int Count => _items.Count;

        public int InsertAt(int index, T value, string actorId)
        {
            ValidateActor(actorId);

            if (index < 0 || index > _items.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(index), "[LedgerArray] index is out of range"
                );

            var itemId = _nextItemId++;

            var evt = new InsertEvent(
                EventId: _nextEventId++,
                ActorId: actorId,
                ItemId: itemId,
                Index: index,
                Value: value
            );

            AppendAndApply(evt);
            return itemId;
        }

        public bool TryRemoveAt(int index, string actorId)
        {
            ValidateActor(actorId);

            if (index < 0 || index >= _items.Count)
                return false;

            var itemId = _items[index].ItemId;

            var evt = new RemoveEvent(
                EventId: _nextEventId++,
                ActorId: actorId,
                ItemId: itemId
            );

            AppendAndApply(evt);
            return true;
        }

        // Clear is terminal for all active items.
        // Ownership and values remain inspectable only via ledger replay.
        public void Clear(string actorId)
        {
            ValidateActor(actorId);

            if (_items.Count == 0)
                return;

            var evt = new ClearEvent(
                EventId: _nextEventId++,
                ActorId: actorId
            );

            AppendAndApply(evt);
        }

        public bool TryGetAt(int index, out T value)
        {
            if (index < 0 || index >= _items.Count)
            {
                value = default!;
                return false;
            }

            value = _items[index].Value;
            return true;
        }

        public bool TryGetAt(int index, out T value, out string ownerId)
        {
            // Use the existing TryGetAt for the value
            if (!TryGetAt(index, out value))
            {
                ownerId = string.Empty;
                return false;
            }

            // If value exists, fetch the owner from _items
            ownerId = _items[index].OwnerId;
            return true;
        }

        public int InsertFirst(T value, string actorId)
        {
            return InsertAt(0, value, actorId);
        }

        public int InsertLast(T value, string actorId)
        {
            return InsertAt(_items.Count, value, actorId);
        }

        public bool TryInsertBefore(int existingItemId, T value, string actorId, out int newItemId)
        {
            newItemId = default;

            // Find the index of the item with the given ItemId
            if (!_indexByItemId.TryGetValue(existingItemId, out var index))
                return false;

            // Insert at that index
            newItemId = InsertAt(index, value, actorId);
            return true;
        }

        public bool TryInsertAfter(int existingItemId, T value, string actorId, out int newItemId)
        {
            newItemId = default;

            // Find the index of the item with the given ItemId
            if (!_indexByItemId.TryGetValue(existingItemId, out var index))
                return false;

            // Insert after that index
            newItemId = InsertAt(index + 1, value, actorId);
            return true;
        }

        public bool TryInsertBeforeValue(T existingValue, T newValue, string actorId, out int newItemId)
        {
            newItemId = default;

            // Find the index of the first item with matching value
            var index = _items.FindIndex(i => EqualityComparer<T>.Default.Equals(i.Value, existingValue));
            if (index == -1)
                return false;

            newItemId = InsertAt(index, newValue, actorId);
            return true;
        }

        public bool TryInsertAfterValue(T existingValue, T newValue, string actorId, out int newItemId)
        {
            newItemId = default;

            // Find the index of the first item with matching value
            var index = _items.FindIndex(i => EqualityComparer<T>.Default.Equals(i.Value, existingValue));
            if (index == -1)
                return false;

            newItemId = InsertAt(index + 1, newValue, actorId);
            return true;
        }

        public int IndexOf(T value)
        {
            return _items.FindIndex(i => EqualityComparer<T>.Default.Equals(i.Value, value));
        }

        public void SetValueAt(int index, T newValue, string actorId)
        {
            ValidateActor(actorId);

            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "[LedgerArray] index is out of range");

            var item = _items[index];
            var evt = new UpdateEvent(
                EventId: _nextEventId++,
                ActorId: actorId,
                ItemId: item.ItemId,
                OldValue: item.Value,
                NewValue: newValue
            );

            AppendAndApply(evt);
        }

        private void AppendAndApply(ArrayEvent evt)
        {
            // Always apply to live _items
            Apply(evt);

            // Only append to ledger if tracking is enabled
            if (_trackingEnabled)
            {
                _ledger.Add(evt);
            }
        }

        private void Apply(ArrayEvent evt)
        {
            switch (evt)
            {
                case InsertEvent insert:
                    ApplyInsert(insert);
                    break;

                case RemoveEvent remove:
                    ApplyRemove(remove);
                    break;

                case ClearEvent:
                    ApplyClear();
                    break;

                case UpdateEvent update:
                    ApplyUpdate(update);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"[LedgerArray] Unknown event type {evt.GetType().Name}"
                    );
            }
        }

        private void ApplyInsert(InsertEvent evt)
        {
            _items.Insert(evt.Index, new ArrayItem(
                ItemId: evt.ItemId,
                OwnerId: evt.ActorId,
                Value: evt.Value
            ));

            RebuildIndex();
        }

        private void ApplyRemove(RemoveEvent evt)
        {
            if (!_indexByItemId.TryGetValue(evt.ItemId, out var index))
                throw new InvalidOperationException(
                    $"[LedgerArray] Attempted to remove unknown ItemId {evt.ItemId}"
                );

            _items.RemoveAt(index);
            _indexByItemId.Remove(evt.ItemId);
            RebuildIndex();
        }

        private void ApplyClear()
        {
            _items.Clear();
            _indexByItemId.Clear();
        }

        private void ApplyUpdate(UpdateEvent evt)
        {
            if (!_indexByItemId.TryGetValue(evt.ItemId, out var index))
                throw new InvalidOperationException(
                    $"[LedgerArray] Attempted to update unknown ItemId {evt.ItemId}"
                );

            _items[index] = _items[index] with { Value = evt.NewValue };
        }

        private void RebuildIndex()
        {
            _indexByItemId.Clear();

            for (int i = 0; i < _items.Count; i++)
            {
                _indexByItemId[_items[i].ItemId] = i;
            }
        }

        private static void ValidateActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException("[LedgerArray] actorId must be non-empty", nameof(actorId));
        }

        private sealed record ArrayItem(
            int ItemId,
            string OwnerId,
            T Value
        );

        public readonly record struct ItemOwnership(
            int ItemId,
            string OwnerId
        );

        public abstract record ArrayEvent(
            int EventId,
            string ActorId
        );

        public sealed record InsertEvent(
            int EventId,
            string ActorId,
            int ItemId,
            int Index,
            T Value
        ) : ArrayEvent(EventId, ActorId);

        public sealed record RemoveEvent(
            int EventId,
            string ActorId,
            int ItemId
        ) : ArrayEvent(EventId, ActorId);

        public sealed record ClearEvent(
            int EventId,
            string ActorId
        ) : ArrayEvent(EventId, ActorId);

        public sealed record UpdateEvent(
            int EventId,
            string ActorId,
            int ItemId,
            T OldValue,
            T NewValue
        ) : ArrayEvent(EventId, ActorId);
    }
}
