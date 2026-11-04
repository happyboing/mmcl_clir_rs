namespace MMCL_CLIRRS;

public enum ScheduledCheckKind
{
    GracePeriodEnds,
    ReservationEnds
}

public sealed record ScheduledCheck(DateTime Due, ScheduledCheckKind Kind, Reservation Reservation);
