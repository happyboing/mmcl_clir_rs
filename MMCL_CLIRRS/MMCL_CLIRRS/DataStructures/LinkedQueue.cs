namespace MMCL_CLIRRS;

public class LinkedQueue<T>
{
    private class Node
    {
        public T Value;
        public Node? Next;

        public Node(T value)
        {
            Value = value;
            Next = null;
        }
    }

    private Node? _front;
    private Node? _back;

    public int Count { get; private set; }
    public bool IsEmpty => _front == null;

    public void Enqueue(T item)
    {
        var newNode = new Node(item);

        if (_back == null)
        {
            _front = newNode;
            _back = newNode;
        }
        else
        {
            _back.Next = newNode;
            _back = newNode;
        }

        Count++;
    }

    public T Dequeue()
    {
        if (_front == null)
            throw new InvalidOperationException("The queue is empty.");

        T value = _front.Value;
        _front = _front.Next;
        if (_front == null) _back = null;

        Count--;
        return value;
    }

    public List<T> ToList()
    {
        var items = new List<T>();
        for (Node? node = _front; node != null; node = node.Next)
            items.Add(node.Value);
        return items;
    }

    public T Peek()
    {
        if (_front == null)
            throw new InvalidOperationException("The queue is empty.");
        return _front.Value;
    }
}
