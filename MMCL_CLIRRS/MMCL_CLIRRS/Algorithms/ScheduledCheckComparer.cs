namespace MMCL_CLIRRS;

public class ScheduledCheckComparer : IComparer<ScheduledCheck>
{
    public int Compare(ScheduledCheck? a, ScheduledCheck? b)
    {
        int byDue = a!.Due.CompareTo(b!.Due);
        return byDue != 0 ? byDue : a.Reservation.ReservationId.CompareTo(b.Reservation.ReservationId);
    }
}
