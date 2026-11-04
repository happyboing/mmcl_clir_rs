namespace MMCL_CLIRRS;

public class AvailabilityService
{
    public static readonly TimeSpan OpeningTime = TimeSpan.Zero;
    public static readonly TimeSpan ClosingTime = TimeSpan.FromHours(23);

    private readonly ReservationManagementService _management;

    public AvailabilityService(ReservationManagementService management)
    {
        _management = management;
    }

    public List<TimeWindow> GetFreeWindows(string spotOrRoom, DateTime date, TimeSpan from, TimeSpan to)
    {
        var free = new List<TimeWindow>();
        TimeSpan cursor = from;

        foreach (Reservation booking in _management.GetBookedSlots(spotOrRoom, date))
        {
            if (booking.EndTime <= cursor) continue;
            if (booking.StartTime >= to) break;

            if (booking.StartTime > cursor)
                free.Add(new TimeWindow(cursor, booking.StartTime));

            cursor = booking.EndTime;
        }

        if (cursor < to)
            free.Add(new TimeWindow(cursor, to));

        return free;
    }

    public TimeWindow? FindNextFreeSlot(string spotOrRoom, DateTime date,
                                        TimeSpan duration, TimeSpan notBefore)
    {
        if (duration <= TimeSpan.Zero) return null;

        foreach (TimeWindow window in GetFreeWindows(spotOrRoom, date, notBefore, ClosingTime))
        {
            if (window.Duration >= duration)
                return new TimeWindow(window.Start, window.Start + duration);
        }
        return null;
    }
}
