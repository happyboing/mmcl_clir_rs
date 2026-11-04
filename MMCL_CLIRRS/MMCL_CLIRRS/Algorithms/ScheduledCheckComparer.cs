namespace MMCL_CLIRRS;

// Orders heap entries by the moment they are due. If two are due at the same
// moment, the reservation made first (smaller ID) goes first: first-come, first-served.
public class ScheduledCheckComparer : IComparer<ScheduledCheck>
{
    public int Compare(ScheduledCheck? a, ScheduledCheck? b)
    {
        int byDue = a!.Due.CompareTo(b!.Due);
        return byDue != 0 ? byDue : a.Reservation.ReservationId.CompareTo(b.Reservation.ReservationId);
    }
}
