namespace MMCL_CLIRRS;

public enum ScheduledCheckKind
{
    GracePeriodEnds,    // an Active reservation must be occupied by now, or it is released
    ReservationEnds     // an Occupied reservation has reached its end time, so it is completed
}

// One entry in the monitor's min-heap: "at this moment, look at this reservation".
public sealed record ScheduledCheck(DateTime Due, ScheduledCheckKind Kind, Reservation Reservation);
