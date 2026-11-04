namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #2: Reservation Scheduling and Availability
//  Answers "when is this seat or room free?".
//
//  Algorithm: a single left-to-right SWEEP over the spot's bookings.
//  GetBookedSlots() already returns them sorted by start time (Merge Sort),
//  so one pass is enough: keep a "cursor" at the end of the last booking,
//  and every gap between the cursor and the next booking is a free window.
//  Cost after sorting: O(k) for k bookings on that spot and date.
// ============================================================
public class AvailabilityService
{
    // The reservation dialog allows times up to 23:00.
    public static readonly TimeSpan OpeningTime = TimeSpan.Zero;
    public static readonly TimeSpan ClosingTime = TimeSpan.FromHours(23);

    private readonly ReservationManagementService _management;

    public AvailabilityService(ReservationManagementService management)
    {
        _management = management;
    }

    // Every free window for this spot on this date between 'from' and 'to', earliest first.
    public List<TimeWindow> GetFreeWindows(string spotOrRoom, DateTime date, TimeSpan from, TimeSpan to)
    {
        var free = new List<TimeWindow>();
        TimeSpan cursor = from;

        foreach (Reservation booking in _management.GetBookedSlots(spotOrRoom, date))
        {
            if (booking.EndTime <= cursor) continue;   // finished before the part we care about
            if (booking.StartTime >= to) break;        // starts after it: nothing more to check

            if (booking.StartTime > cursor)
                free.Add(new TimeWindow(cursor, booking.StartTime));

            cursor = booking.EndTime;                  // continue after this booking
        }

        if (cursor < to)
            free.Add(new TimeWindow(cursor, to));

        return free;
    }

    // The first slot of the wanted length that starts at or after 'notBefore'.
    // Returns null when the rest of the day has no gap that long.
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
