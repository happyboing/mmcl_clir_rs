namespace MMCL_CLIRRS;

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

    public void Push(T item)
    {
        if (Count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);

        _items[Count] = item;
        SiftUp(Count);
        Count++;
    }

    public T Peek()
    {
        if (Count == 0) throw new InvalidOperationException("The heap is empty.");
        return _items[0];
    }

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

    private void SiftUp(int index)
    {
        T item = _items[index];

        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_comparer.Compare(item, _items[parent]) >= 0) break;

            _items[index] = _items[parent];
            index = parent;
        }

        _items[index] = item;
    }

    private void SiftDown(int index)
    {
        T item = _items[index];
        int half = Count / 2;

        while (index < half)
        {
            int child = 2 * index + 1;
            int right = child + 1;
            if (right < Count && _comparer.Compare(_items[right], _items[child]) < 0)
                child = right;

            if (_comparer.Compare(item, _items[child]) <= 0) break;

            _items[index] = _items[child];
            index = child;
        }

        _items[index] = item;
    }
}
