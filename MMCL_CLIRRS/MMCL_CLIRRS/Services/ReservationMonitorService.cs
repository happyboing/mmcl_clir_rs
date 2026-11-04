namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #2: Reservation Scheduling and Availability
//  The 5-minute grace period monitor.
//
//  What it does:
//    1. Watches every reservation. If an ACTIVE reservation is still not
//       occupied 5 minutes after its start time, it is released (marked
//       Expired), so the seat or room shows as Available again, and CLIR
//       personnel are notified.
//    2. When an OCCUPIED reservation reaches its end time, it is marked
//       Completed.
//
//  How it stays fast:
//    Each reservation gets ONE entry in a MIN-HEAP, ordered by the moment
//    it is due. Every second the timer calls ProcessDue(), which only looks
//    at the top of the heap (O(1)) and pops entries that are due (O(log n)
//    each). It never loops over all reservations.
//
//    Pending alerts wait in a QUEUE: the oldest alert is handled first.
//
//  Staying correct when reservations change:
//    Staff can cancel, verify, update or undo a reservation at any time.
//    Instead of searching the heap and deleting entries, the monitor uses
//    "lazy deletion": old entries stay in the heap, and when one comes due
//    the monitor re-checks the reservation. If the entry no longer matches
//    the reservation's current status and time, it is simply thrown away.
//    Every change to a reservation raises ReservationChanged, which schedules
//    a fresh entry, so nothing is ever missed.
//
//  Everything runs on the UI thread (a WinForms timer), so no locking is needed.
// ============================================================
public class ReservationMonitorService
{
    private readonly ReservationSearchService _search;
    private readonly MinHeap<ScheduledCheck> _checks = new MinHeap<ScheduledCheck>(new ScheduledCheckComparer());   // HEAP
    private readonly LinkedQueue<StaffNotification> _alerts = new LinkedQueue<StaffNotification>();                // QUEUE

    // A new alert was raised (the UI plays a sound / shows a badge).
    public event Action<StaffNotification>? AlertRaised;

    // The list of pending alerts changed (raised or acknowledged).
    public event Action? AlertsChanged;

    // Some reservations were released or completed, so seat availability changed.
    public event Action? ScheduleChanged;

    public ReservationMonitorService(IReservationRepository repository, ReservationSearchService search)
    {
        _search = search;

        // Schedule everything that already exists...
        foreach (Reservation reservation in repository.GetAll())
            Schedule(reservation);

        // ...and everything that is added or changed from now on.
        _search.ReservationChanged += Schedule;
    }

    // =========================================================
    //  Monitoring
    // =========================================================

    // Called by the timer. Handles every check whose moment has arrived.
    // Returns how many reservations were released or completed.
    public int ProcessDue(DateTime now)
    {
        int changes = 0;

        while (!_checks.IsEmpty && _checks.Peek().Due <= now)
        {
            ScheduledCheck check = _checks.Pop();

            // Cancelled, verified, rescheduled or already handled since this entry was made?
            if (!IsStillValid(check)) continue;

            if (check.Kind == ScheduledCheckKind.GracePeriodEnds)
            {
                if (ReleaseUnoccupied(check.Reservation, now)) changes++;
            }
            else
            {
                if (_search.MarkCompleted(check.Reservation.ReservationId)) changes++;
            }
        }

        if (changes > 0) ScheduleChanged?.Invoke();
        return changes;
    }

    // Grace period over and nobody came: free the seat and tell the staff.
    private bool ReleaseUnoccupied(Reservation reservation, DateTime now)
    {
        // Marks it Expired and writes it in the activity log (Functionality #4).
        // Expired no longer "holds" the spot, so it is available again.
        if (!_search.MarkExpired(reservation.ReservationId)) return false;

        var alert = new StaffNotification(
            now,
            reservation.ReservationId,
            reservation.UserName,
            reservation.UserIdNumber,
            reservation.SpotOrRoom,
            SchedulingRules.StartMoment(reservation));

        _alerts.Enqueue(alert);
        AlertRaised?.Invoke(alert);
        AlertsChanged?.Invoke();
        return true;
    }

    // =========================================================
    //  Scheduling (heap entries)
    // =========================================================

    // Add a fresh heap entry for this reservation, if its status needs watching.
    private void Schedule(Reservation reservation)
    {
        ScheduledCheck? check = BuildCheck(reservation);
        if (check != null) _checks.Push(check);
    }

    // What should be checked for this reservation, and when? (null = nothing to watch)
    private static ScheduledCheck? BuildCheck(Reservation reservation)
    {
        switch (reservation.Status)
        {
            case ReservationStatus.Active:
                return new ScheduledCheck(SchedulingRules.GraceDeadline(reservation),
                                          ScheduledCheckKind.GracePeriodEnds, reservation);
            case ReservationStatus.Occupied:
                return new ScheduledCheck(SchedulingRules.EndMoment(reservation),
                                          ScheduledCheckKind.ReservationEnds, reservation);
            default:
                return null;   // Cancelled / Expired / Completed: nothing left to watch
        }
    }

    // An entry is valid only if it still matches what the reservation needs right now.
    private static bool IsStillValid(ScheduledCheck check)
    {
        ScheduledCheck? current = BuildCheck(check.Reservation);
        return current != null && current.Kind == check.Kind && current.Due == check.Due;
    }

    // =========================================================
    //  Staff alerts (queue: oldest first)
    // =========================================================

    public int PendingAlertCount => _alerts.Count;

    // Oldest first. Does not remove anything.
    public List<StaffNotification> GetPendingAlerts() => _alerts.ToList();

    // Staff dealt with the oldest alert.
    public StaffNotification? AcknowledgeNext()
    {
        if (_alerts.IsEmpty) return null;

        StaffNotification alert = _alerts.Dequeue();
        AlertsChanged?.Invoke();
        return alert;
    }

    public void AcknowledgeAll()
    {
        if (_alerts.IsEmpty) return;

        while (!_alerts.IsEmpty)
            _alerts.Dequeue();

        AlertsChanged?.Invoke();
    }
}
