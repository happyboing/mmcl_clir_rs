namespace MMCL_CLIRRS;

// ============================================================
//  QUEUE  (Functionality #1)
//
//  A queue is a line at a cashier: FIRST in line = FIRST served.
//    Enqueue = join the back of the line
//    Dequeue = the person at the FRONT leaves and gets served
//
//      front                          back
//        |                              |
//      [Req 1] -> [Req 2] -> [Req 3] -> null
//
//  Built from linked nodes, same idea as LinkedStack.
// ============================================================
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

    // Join the back of the line.
    public void Enqueue(T item)
    {
        var newNode = new Node(item);

        if (_back == null)            // empty line
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

    // Take the person at the FRONT of the line.
    public T Dequeue()
    {
        if (_front == null)
            throw new InvalidOperationException("The queue is empty.");

        T value = _front.Value;
        _front = _front.Next;
        if (_front == null) _back = null;   // line is now empty

        Count--;
        return value;
    }

    // Look at who is first without removing them.
    public T Peek()
    {
        if (_front == null)
            throw new InvalidOperationException("The queue is empty.");
        return _front.Value;
    }
}
