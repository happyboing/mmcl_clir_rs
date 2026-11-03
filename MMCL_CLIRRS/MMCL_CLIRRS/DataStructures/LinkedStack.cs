namespace MMCL_CLIRRS;

// A stack built from linked nodes: the last item pushed is the first one out.
public class LinkedStack<T>
{
    private class Node
    {
        public T Value;
        public Node? Next;
        public Node(T value, Node? next) { Value = value; Next = next; }
    }

    private Node? _top;

    public int Count { get; private set; }
    public bool IsEmpty => _top == null;

    public void Push(T item)
    {
        _top = new Node(item, _top);
        Count++;
    }

    public T Pop()
    {
        if (_top == null) throw new InvalidOperationException("The stack is empty.");
        T value = _top.Value;
        _top = _top.Next;
        Count--;
        return value;
    }

    public T Peek()
    {
        if (_top == null) throw new InvalidOperationException("The stack is empty.");
        return _top.Value;
    }

    // Top (most recent) first.
    public List<T> ToList()
    {
        var items = new List<T>();
        for (Node? node = _top; node != null; node = node.Next)
            items.Add(node.Value);
        return items;
    }
}