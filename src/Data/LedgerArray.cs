using System.Collections.ObjectModel;
using AxiomPlayground.GameFlag;

namespace AxiomPlayground.Data
{
    public sealed class LedgerArray
    {
        private int _nextItemId = 1;
        private int _nextEventId = 1;
        private readonly List<ArrayEvent> _ledger = new();
        private readonly List<ArrayItem> _items = new();
        private readonly Dictionary<int, int> _indexByItemId = new();

        public IEnumerable<object> Values => _items.Select(i => i.Value);

        public IReadOnlyList<ArrayEvent> Ledger => _ledger.AsReadOnly();

        public IReadOnlyList<ItemOwnership> CurrentOwnership =>
            new ReadOnlyCollection<ItemOwnership>(
                _items.Select(i => new ItemOwnership(i.ItemId, i.OwnerId)).ToList()
            );

        private readonly bool _trackingEnabled = GameFlagManager.IsSet(FrameworkGameFlag.Debug);

        public int Count => _items.Count;

        public int InsertAt(int index, object value, string actorId)
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

        public bool TryGetAt(int index, out object value)
        {
            if (index < 0 || index >= _items.Count)
            {
                value = null!;
                return false;
            }

            value = _items[index].Value;
            return true;
        }

        public bool TryGetAt(int index, out object value, out string ownerId)
        {
            if (!TryGetAt(index, out value))
            {
                ownerId = string.Empty;
                return false;
            }

            ownerId = _items[index].OwnerId;
            return true;
        }

        public int InsertFirst(object value, string actorId) => InsertAt(0, value, actorId);

        public int InsertLast(object value, string actorId) => InsertAt(_items.Count, value, actorId);

        public bool TryInsertBefore(int existingItemId, object value, string actorId, out int newItemId)
        {
            newItemId = default;
            if (!_indexByItemId.TryGetValue(existingItemId, out var index))
                return false;

            newItemId = InsertAt(index, value, actorId);
            return true;
        }

        public bool TryInsertAfter(int existingItemId, object value, string actorId, out int newItemId)
        {
            newItemId = default;
            if (!_indexByItemId.TryGetValue(existingItemId, out var index))
                return false;

            newItemId = InsertAt(index + 1, value, actorId);
            return true;
        }

        public bool TryInsertBeforeValue(object existingValue, object newValue, string actorId, out int newItemId)
        {
            newItemId = default;
            var index = _items.FindIndex(i => Equals(i.Value, existingValue));
            if (index == -1)
                return false;

            newItemId = InsertAt(index, newValue, actorId);
            return true;
        }

        public bool TryInsertAfterValue(object existingValue, object newValue, string actorId, out int newItemId)
        {
            newItemId = default;
            var index = _items.FindIndex(i => Equals(i.Value, existingValue));
            if (index == -1)
                return false;

            newItemId = InsertAt(index + 1, newValue, actorId);
            return true;
        }

        public int IndexOf(object value) => _items.FindIndex(i => Equals(i.Value, value));

        public void SetValueAt(int index, object newValue, string actorId)
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
            Apply(evt);
            if (_trackingEnabled)
            {
                _ledger.Add(evt);
            }
        }

        private void Apply(ArrayEvent evt)
        {
            switch (evt)
            {
                case InsertEvent insert: ApplyInsert(insert); break;
                case RemoveEvent remove: ApplyRemove(remove); break;
                case ClearEvent: ApplyClear(); break;
                case UpdateEvent update: ApplyUpdate(update); break;
                default:
                    throw new InvalidOperationException($"[LedgerArray] Unknown event type {evt.GetType().Name}");
            }
        }

        private void ApplyInsert(InsertEvent evt)
        {
            _items.Insert(evt.Index, new ArrayItem(evt.ItemId, evt.ActorId, evt.Value));
            RebuildIndex();
        }

        private void ApplyRemove(RemoveEvent evt)
        {
            if (!_indexByItemId.TryGetValue(evt.ItemId, out var index))
                throw new InvalidOperationException($"[LedgerArray] Attempted to remove unknown ItemId {evt.ItemId}");

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
                throw new InvalidOperationException($"[LedgerArray] Attempted to update unknown ItemId {evt.ItemId}");

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

        private sealed record ArrayItem(int ItemId, string OwnerId, object Value);

        public readonly record struct ItemOwnership(int ItemId, string OwnerId);

        public abstract record ArrayEvent(int EventId, string ActorId);

        public sealed record InsertEvent(int EventId, string ActorId, int ItemId, int Index, object Value)
            : ArrayEvent(EventId, ActorId);

        public sealed record RemoveEvent(int EventId, string ActorId, int ItemId)
            : ArrayEvent(EventId, ActorId);

        public sealed record ClearEvent(int EventId, string ActorId)
            : ArrayEvent(EventId, ActorId);

        public sealed record UpdateEvent(int EventId, string ActorId, int ItemId, object OldValue, object NewValue)
            : ArrayEvent(EventId, ActorId);
    }
}
