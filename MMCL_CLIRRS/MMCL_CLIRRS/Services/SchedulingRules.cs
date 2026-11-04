namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #2: Reservation Scheduling and Availability
//  The time rules in one place, so the monitor, the forms and
//  the reports all use the same numbers.
// ============================================================
public static class SchedulingRules
{
    // From the proposal: a reserved seat that is still unoccupied
    // 5 minutes after the scheduled time is released.
    public static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(5);

    // The exact moment (date + time) a reservation starts / ends.
    public static DateTime StartMoment(Reservation r) => r.Date.Date + r.StartTime;
    public static DateTime EndMoment(Reservation r) => r.Date.Date + r.EndTime;

    // The moment an unclaimed reservation is released: start + 5 minutes.
    // A reservation shorter than the grace period is released when it ends instead.
    public static DateTime GraceDeadline(Reservation r)
    {
        DateTime deadline = StartMoment(r) + GracePeriod;
        DateTime end = EndMoment(r);
        return deadline < end ? deadline : end;
    }
}
