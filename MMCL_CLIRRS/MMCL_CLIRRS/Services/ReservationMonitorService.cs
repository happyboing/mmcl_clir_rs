namespace MMCL_CLIRRS;

public class ReservationMonitorService
{
    private readonly ReservationSearchService _search;
    private readonly MinHeap<ScheduledCheck> _checks = new MinHeap<ScheduledCheck>(new ScheduledCheckComparer());
    private readonly LinkedQueue<StaffNotification> _alerts = new LinkedQueue<StaffNotification>();

    public event Action<StaffNotification>? AlertRaised;

    public event Action? AlertsChanged;

    public event Action? ScheduleChanged;

    public ReservationMonitorService(IReservationRepository repository, ReservationSearchService search)
    {
        _search = search;

        foreach (Reservation reservation in repository.GetAll())
            Schedule(reservation);

        _search.ReservationChanged += Schedule;
    }

    public int ProcessDue(DateTime now)
    {
        int changes = 0;

        while (!_checks.IsEmpty && _checks.Peek().Due <= now)
        {
            ScheduledCheck check = _checks.Pop();

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

    private bool ReleaseUnoccupied(Reservation reservation, DateTime now)
    {
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

    private void Schedule(Reservation reservation)
    {
        ScheduledCheck? check = BuildCheck(reservation);
        if (check != null) _checks.Push(check);
    }

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
                return null;
        }
    }

    private static bool IsStillValid(ScheduledCheck check)
    {
        ScheduledCheck? current = BuildCheck(check.Reservation);
        return current != null && current.Kind == check.Kind && current.Due == check.Due;
    }

    public int PendingAlertCount => _alerts.Count;

    public List<StaffNotification> GetPendingAlerts() => _alerts.ToList();

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
