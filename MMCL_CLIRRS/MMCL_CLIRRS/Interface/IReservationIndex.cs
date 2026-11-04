namespace MMCL_CLIRRS;

public interface IReservationIndex
{
    void Insert(Reservation reservation);
    bool Remove(Reservation reservation);
}

public interface IReservationIndex<TKey> : IReservationIndex
{
    IReadOnlyList<Reservation> Find(TKey key);
}