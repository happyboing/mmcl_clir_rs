namespace MMCL_CLIRRS;

public class ReservationLinkedList : IReservationRepository
{
    private class Node
    {
        public Reservation Data;
        public Node? Next;

        public Node(Reservation data)
        {
            Data = data;
            Next = null;
        }
    }

    private Node? _head;
    private Node? _tail;

    public int Count { get; private set; }

    public void Add(Reservation reservation)
    {
        var newNode = new Node(reservation);

        if (_head == null)
        {
            _head = newNode;
            _tail = newNode;
        }
        else
        {
            _tail!.Next = newNode;
            _tail = newNode;
        }

        Count++;
    }

    public IEnumerable<Reservation> GetAll()
    {
        for (Node? current = _head; current != null; current = current.Next)
            yield return current.Data;
    }

    public Reservation? GetById(int reservationId)
    {
        for (Node? current = _head; current != null; current = current.Next)
        {
            if (current.Data.ReservationId == reservationId)
                return current.Data;
        }
        return null;
    }
}
