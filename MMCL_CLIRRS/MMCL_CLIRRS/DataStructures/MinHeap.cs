namespace MMCL_CLIRRS;

// ============================================================
//  MIN-HEAP / PRIORITY QUEUE  (Functionality #2)
//
//  A heap is a "complete binary tree" kept inside a plain array.
//  The rule: every parent is SMALLER than (or equal to) its children,
//  so the smallest item is always at the very top (index 0).
//
//             [10:00]                 array: [10:00, 10:05, 10:30, 11:00, 10:45]
//             /     \                         0      1      2      3      4
//        [10:05]   [10:30]
//         /    \                parent of i  = (i - 1) / 2
//    [11:00] [10:45]            children of i = 2i + 1  and  2i + 2
//
//  Why the grace-period monitor uses it:
//    Peek  (what is due next?)        -> O(1)
//    Push  (schedule a new deadline)  -> O(log n)
//    Pop   (take the earliest one)    -> O(log n)
//  The monitor only ever needs the EARLIEST deadline, so it never has to
//  scan every reservation each second.
// ============================================================
public class MinHeap<T>
{
    private T[] _items = new T[16];
    private readonly IComparer<T> _comparer;

    public int Count { get; private set; }
    public bool IsEmpty => Count == 0;

    public MinHeap(IComparer<T>? comparer = null)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    // Add the item at the bottom, then let it float UP until the rule holds again.
    public void Push(T item)
    {
        if (Count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);

        _items[Count] = item;
        SiftUp(Count);
        Count++;
    }

    // Look at the smallest item without removing it.
    public T Peek()
    {
        if (Count == 0) throw new InvalidOperationException("The heap is empty.");
        return _items[0];
    }

    // Remove the smallest item: move the LAST item to the top, then let it sink DOWN.
    public T Pop()
    {
        if (Count == 0) throw new InvalidOperationException("The heap is empty.");

        T smallest = _items[0];
        Count--;

        if (Count > 0)
        {
            _items[0] = _items[Count];
            _items[Count] = default!;
            SiftDown(0);
        }
        else
        {
            _items[0] = default!;
        }

        return smallest;
    }

    // Move the item at 'index' up while it is smaller than its parent.
    private void SiftUp(int index)
    {
        T item = _items[index];

        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_comparer.Compare(item, _items[parent]) >= 0) break;

            _items[index] = _items[parent];   // pull the parent down one level
            index = parent;
        }

        _items[index] = item;
    }

    // Move the item at 'index' down while a child is smaller than it.
    private void SiftDown(int index)
    {
        T item = _items[index];
        int half = Count / 2;                  // indexes below this have at least one child

        while (index < half)
        {
            int child = 2 * index + 1;         // left child
            int right = child + 1;
            if (right < Count && _comparer.Compare(_items[right], _items[child]) < 0)
                child = right;                 // pick the smaller of the two children

            if (_comparer.Compare(item, _items[child]) <= 0) break;

            _items[index] = _items[child];     // pull the smaller child up one level
            index = child;
        }

        _items[index] = item;
    }
}
