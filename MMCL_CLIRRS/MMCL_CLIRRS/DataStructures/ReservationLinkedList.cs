namespace MMCL_CLIRRS;

// ============================================================
//  LINKED LIST  (Functionality #1)
//
//  Think of a train: every train car (a "node") carries one
//  passenger (a Reservation) and is hooked to the NEXT car.
//  We only remember the first car (_head) and the last car (_tail).
//
//      _head                              _tail
//        |                                  |
//      [Res A | next] -> [Res B | next] -> [Res C | next] -> null
//
//  This class is the "master list" of every reservation.
//  It implements IReservationRepository, so it replaces
//  MockReservationRepository without changing anyone else's code.
// ============================================================
public class ReservationLinkedList : IReservationRepository
{
    // One train car: holds a reservation + a link to the next car.
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

    private Node? _head;   // first node (null when the list is empty)
    private Node? _tail;   // last node  (lets us add at the end quickly)

    public int Count { get; private set; }

    // Add a reservation at the END of the chain.
    public void Add(Reservation reservation)
    {
        var newNode = new Node(reservation);

        if (_head == null)            // empty list: the new node is both first and last
        {
            _head = newNode;
            _tail = newNode;
        }
        else                          // otherwise hook it after the current last node
        {
            _tail!.Next = newNode;
            _tail = newNode;
        }

        Count++;
    }

    // Walk from the first node to the last, handing back one reservation at a time.
    public IEnumerable<Reservation> GetAll()
    {
        for (Node? current = _head; current != null; current = current.Next)
            yield return current.Data;
    }

    // Walk the chain until we find the matching ID.
    public Reservation? GetById(int reservationId)
    {
        for (Node? current = _head; current != null; current = current.Next)
        {
            if (current.Data.ReservationId == reservationId)
                return current.Data;
        }
        return null;   // reached the end: not found
    }
}
