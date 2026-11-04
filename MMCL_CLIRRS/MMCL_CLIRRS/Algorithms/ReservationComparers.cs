namespace MMCL_CLIRRS;

public class ReservationIdComparer : IComparer<Reservation>
{
    public int Compare(Reservation? a, Reservation? b)
        => a!.ReservationId.CompareTo(b!.ReservationId);
}

public class ReservationDateTimeComparer : IComparer<Reservation>
{
    public int Compare(Reservation? a, Reservation? b)
    {
        int byDate = a!.Date.CompareTo(b!.Date);
        return byDate != 0 ? byDate : a.StartTime.CompareTo(b.StartTime);
    }
}