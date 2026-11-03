namespace MMCL_CLIRRS;

// Anything that can index reservations: add, remove, look up.
public interface IReservationIndex
{
    void Insert(Reservation reservation);
    bool Remove(Reservation reservation);
}

public interface IReservationIndex<TKey> : IReservationIndex
{
    IReadOnlyList<Reservation> Find(TKey key);
}