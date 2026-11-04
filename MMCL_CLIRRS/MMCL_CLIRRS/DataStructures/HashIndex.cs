namespace MMCL_CLIRRS;

public class HashIndex<TKey> : IReservationIndex<TKey> where TKey : notnull
{
    private class Entry
    {
        public TKey Key;
        public List<Reservation> Items = new List<Reservation>();
        public Entry? Next;
        public Entry(TKey key) { Key = key; }
    }

    private const double MaxLoadFactor = 0.75;

    private Entry?[] _buckets;
    private int _distinctKeys;
    private readonly Func<Reservation, TKey> _keySelector;
    private readonly IEqualityComparer<TKey> _comparer;

    public int Count { get; private set; }

    public HashIndex(Func<Reservation, TKey> keySelector,
                     IEqualityComparer<TKey>? comparer = null,
                     int initialCapacity = 16)
    {
        _keySelector = keySelector;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _buckets = new Entry?[initialCapacity];
    }

    public void Insert(Reservation reservation)
    {
        TKey key = _keySelector(reservation);
        Entry? entry = FindEntry(key);

        if (entry == null)
        {
            if (_distinctKeys + 1 > _buckets.Length * MaxLoadFactor)
                Resize();

            entry = new Entry(key);
            int index = BucketIndex(key, _buckets.Length);
            entry.Next = _buckets[index];
            _buckets[index] = entry;
            _distinctKeys++;
        }

        entry.Items.Add(reservation);
        Count++;
    }

    public bool Remove(Reservation reservation)
    {
        TKey key = _keySelector(reservation);
        int index = BucketIndex(key, _buckets.Length);

        Entry? previous = null;
        Entry? current = _buckets[index];

        while (current != null)
        {
            if (_comparer.Equals(current.Key, key))
            {
                if (!current.Items.Remove(reservation)) return false;
                Count--;

                if (current.Items.Count == 0)
                {
                    if (previous == null) _buckets[index] = current.Next;
                    else previous.Next = current.Next;
                    _distinctKeys--;
                }
                return true;
            }
            previous = current;
            current = current.Next;
        }
        return false;
    }

    public IReadOnlyList<Reservation> Find(TKey key)
    {
        Entry? entry = FindEntry(key);
        if (entry != null) return entry.Items;
        return Array.Empty<Reservation>();
    }

    private int BucketIndex(TKey key, int bucketCount)
        => (_comparer.GetHashCode(key) & 0x7FFFFFFF) % bucketCount;

    private Entry? FindEntry(TKey key)
    {
        Entry? current = _buckets[BucketIndex(key, _buckets.Length)];
        while (current != null)
        {
            if (_comparer.Equals(current.Key, key)) return current;
            current = current.Next;
        }
        return null;
    }

    private void Resize()
    {
        Entry?[] bigger = new Entry?[_buckets.Length * 2];

        foreach (Entry? head in _buckets)
        {
            Entry? current = head;
            while (current != null)
            {
                Entry? next = current.Next;
                int index = BucketIndex(current.Key, bigger.Length);
                current.Next = bigger[index];
                bigger[index] = current;
                current = next;
            }
        }
        _buckets = bigger;
    }
}