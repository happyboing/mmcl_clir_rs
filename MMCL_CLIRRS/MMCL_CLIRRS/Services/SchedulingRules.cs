namespace MMCL_CLIRRS;

public static class SchedulingRules
{
    public static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(5);

    public static DateTime StartMoment(Reservation r) => r.Date.Date + r.StartTime;
    public static DateTime EndMoment(Reservation r) => r.Date.Date + r.EndTime;

    public static DateTime GraceDeadline(Reservation r)
    {
        DateTime deadline = StartMoment(r) + GracePeriod;
        DateTime end = EndMoment(r);
        return deadline < end ? deadline : end;
    }
}
