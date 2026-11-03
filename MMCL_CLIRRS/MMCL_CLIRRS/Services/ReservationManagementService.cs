namespace MMCL_CLIRRS;

// ============================================================
// Functionality #1: Seat and Room Reservation Management
// Functionality #2: Reservation Scheduling and Availability
//
// The service owns the scheduling rules. Forms only collect input
// and display the current state returned by this class.
// ============================================================
public class ReservationManagementService
{
    private static readonly TimeSpan NoShowGracePeriod = TimeSpan.FromMinutes(5);

    private readonly ReservationLinkedList _reservations;
    private readonly ReservationSearchService _search;
    private readonly LinkedQueue<ReservationRequest> _requestQueue = new();
    private readonly object _schedulingGate = new();
    private readonly List<ReservationExpirationNotice> _pendingExpirationNotices = new();
    private int _nextId;

    // Raised after a reservation is automatically expired. The main menu
    // drives ProcessDueReservations on the WinForms UI thread, so subscribers
    // can safely update their forms and display a staff notification.
    public event Action<ReservationExpirationNotice>? ReservationExpired;

    public ReservationManagementService(ReservationLinkedList reservations,
                                        ReservationSearchService search)
    {
        _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        _search = search ?? throw new ArgumentNullException(nameof(search));

        // New reservation IDs continue after the biggest existing ID.
        _nextId = _reservations.GetAll()
                               .Select(r => r.ReservationId)
                               .DefaultIfEmpty(1000)
                               .Max() + 1;
    }

    // =========================================================
    // MAKING A RESERVATION - Queue / First-Come, First-Served
    // =========================================================

    public OperationResult SubmitRequest(ReservationRequest request)
    {
        if (request == null)
            return new OperationResult(false, "A reservation request is required.");

        // Release any no-show reservations before checking availability.
        ProcessDueReservations();

        lock (_schedulingGate)
        {
            string? problem = Validate(request, DateTime.Now);
            if (problem != null)
                return new OperationResult(false, problem);

            // The scheduling lock makes enqueue + processing atomic. If two
            // callers submit at almost the same time, the request that enters
            // this queue first is processed first and gets the slot first.
            _requestQueue.Enqueue(request);

            OperationResult result = new OperationResult(false, "Nothing was processed.");
            while (!_requestQueue.IsEmpty)
                result = ProcessNextRequest();

            return result;
        }
    }

    private OperationResult ProcessNextRequest()
    {
        ReservationRequest request = _requestQueue.Dequeue();

        // Re-check at processing time. The availability shown in a form may
        // have changed since the user first selected the seat or room.
        Reservation? conflict = FindConflict(request.SpotOrRoom, request.Date,
                                             request.StartTime, request.EndTime);
        if (conflict != null)
        {
            return new OperationResult(false,
                $"{request.SpotOrRoom} is already reserved from {Clock(conflict.StartTime)} " +
                $"to {Clock(conflict.EndTime)} on that date. Please pick another time or spot.");
        }

        var reservation = new Reservation(
            _nextId++, request.UserName.Trim(), request.UserIdNumber.Trim(), request.UserType,
            request.Department.Trim(), request.Program.Trim(), request.YearLevel,
            request.SpotOrRoom.Trim(), request.IsDiscussionRoom,
            request.Date.Date, request.StartTime, request.EndTime);

        _reservations.Add(reservation);
        _search.Index(reservation);

        return new OperationResult(true,
            $"Reservation #{reservation.ReservationId} confirmed!\n\n" +
            $"{reservation.SpotOrRoom}\n" +
            $"{reservation.Date:ddd, dd MMM yyyy}, {Clock(reservation.StartTime)} - {Clock(reservation.EndTime)}");
    }

    private static string? Validate(ReservationRequest r, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(r.UserName)) return "Please enter your name.";
        if (string.IsNullOrWhiteSpace(r.UserIdNumber)) return "Please enter your student / faculty ID number.";
        if (string.IsNullOrWhiteSpace(r.Department)) return "Please enter your department.";
        if (string.IsNullOrWhiteSpace(r.Program)) return "Please enter your program.";
        if (!Enum.IsDefined(typeof(UserType), r.UserType)) return "Please choose a valid user type.";

        if (r.UserType == UserType.Student && (r.YearLevel < 1 || r.YearLevel > 6))
            return "Please enter a valid year level.";

        string spot = r.SpotOrRoom?.Trim() ?? string.Empty;
        if (spot.Length == 0 || !ClirLayout.Exists(spot))
            return "Please choose a seat or room from the list.";

        bool isDiscussionRoom = ClirLayout.GetDiscussionRooms().Contains(spot);
        if (r.IsDiscussionRoom != isDiscussionRoom)
            return "The selected location does not match the chosen seat / discussion-room category.";

        if (r.StartTime < TimeSpan.Zero || r.StartTime >= TimeSpan.FromDays(1) ||
            r.EndTime <= TimeSpan.Zero || r.EndTime >= TimeSpan.FromDays(1))
            return "Please choose valid reservation times within the same day.";
        if (r.EndTime <= r.StartTime)
            return "The end time must be after the start time.";

        DateTime today = now.Date;
        if (r.UserType == UserType.Student && r.Date.Date != today)
            return "Students can only reserve for today.";
        if (r.Date.Date < today)
            return "You cannot reserve a date that has already passed.";

        if (r.Date.Date == today && r.StartTime < now.TimeOfDay)
            return "That start time has already passed. Please choose a later time.";

        return null;
    }

    // =========================================================
    // AVAILABILITY
    // =========================================================

    // This is always calculated from the current reservation index. No
    // availability result is cached, so a newly booked or released slot is
    // reflected on the next query.
    public bool IsAvailable(string spotOrRoom, DateTime date, TimeSpan start, TimeSpan end)
    {
        if (string.IsNullOrWhiteSpace(spotOrRoom) || !ClirLayout.Exists(spotOrRoom.Trim()) ||
            start < TimeSpan.Zero || end <= start || end >= TimeSpan.FromDays(1))
            return false;

        lock (_schedulingGate)
            return FindConflict(spotOrRoom.Trim(), date.Date, start, end) == null;
    }

    // Reservations that are still holding a location for the selected date,
    // returned chronologically using the project's Merge Sort implementation.
    public List<Reservation> GetBookedSlots(string spotOrRoom, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(spotOrRoom))
            return new List<Reservation>();

        lock (_schedulingGate)
        {
            var booked = new List<Reservation>();
            foreach (Reservation r in _search.SearchBySpot(spotOrRoom.Trim()))
            {
                if (IsHoldingSpot(r) && r.Date.Date == date.Date)
                    booked.Add(r);
            }

            return MergeSorter.Sort(booked, new ReservationDateTimeComparer());
        }
    }

    private Reservation? FindConflict(string spotOrRoom, DateTime date,
                                      TimeSpan start, TimeSpan end)
    {
        foreach (Reservation other in GetBookedSlots(spotOrRoom, date))
        {
            // Adjacent reservations are allowed: [09:00, 10:00) and
            // [10:00, 11:00) do not overlap.
            if (start < other.EndTime && other.StartTime < end)
                return other;
        }
        return null;
    }

    // Active reservations are held until cancelled or expired. Verified
    // occupants continue holding the location until their end time.
    private static bool IsHoldingSpot(Reservation r)
        => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Occupied;

    // =========================================================
    // AUTOMATIC SCHEDULING / FIVE-MINUTE NO-SHOW MONITOR
    // =========================================================

    // Call this periodically while the application is running. Active
    // reservations expire at scheduled start + five minutes if they were not
    // verified as Occupied. Occupied reservations complete at their end time.
    // The optional time parameter also makes the scheduling rules testable.
    public List<ReservationExpirationNotice> ProcessDueReservations(DateTime? currentTime = null)
    {
        DateTime now = currentTime ?? DateTime.Now;
        var newlyExpired = new List<ReservationExpirationNotice>();

        lock (_schedulingGate)
        {
            // Snapshot the linked list because each status change updates the
            // search indexes while this pass is running.
            foreach (Reservation reservation in _reservations.GetAll().ToList())
            {
                if (reservation.Status == ReservationStatus.Active)
                {
                    DateTime scheduledStart = reservation.Date.Date.Add(reservation.StartTime);
                    DateTime graceDeadline = scheduledStart.Add(NoShowGracePeriod);

                    if (now >= graceDeadline && _search.MarkExpired(reservation.ReservationId))
                    {
                        var notice = new ReservationExpirationNotice(
                            reservation.ReservationId,
                            reservation.UserName,
                            reservation.SpotOrRoom,
                            scheduledStart,
                            now);

                        newlyExpired.Add(notice);
                        _pendingExpirationNotices.Add(notice);
                    }
                }
                else if (reservation.Status == ReservationStatus.Occupied)
                {
                    DateTime scheduledEnd = reservation.Date.Date.Add(reservation.EndTime);
                    if (now >= scheduledEnd)
                        _search.MarkCompleted(reservation.ReservationId);
                }
            }
        }

        // Notify outside the lock so form handlers can safely query the service.
        foreach (ReservationExpirationNotice notice in newlyExpired)
            ReservationExpired?.Invoke(notice);

        return newlyExpired;
    }

    // Pending notices remain available if no staff screen was open at the
    // moment a no-show reservation expired. Opening the staff screen consumes
    // them, while live event handlers acknowledge their own notice.
    public List<ReservationExpirationNotice> GetPendingExpirationNotifications()
    {
        lock (_schedulingGate)
        {
            var pending = new List<ReservationExpirationNotice>(_pendingExpirationNotices);
            _pendingExpirationNotices.Clear();
            return pending;
        }
    }

    public void AcknowledgeExpirationNotification(int reservationId)
    {
        lock (_schedulingGate)
            _pendingExpirationNotices.RemoveAll(n => n.ReservationId == reservationId);
    }

    // =========================================================
    // CANCELLING (by student / faculty ID number)
    // =========================================================

    public List<Reservation> GetActiveReservationsOf(string idNumber)
    {
        lock (_schedulingGate)
        {
            var mine = new List<Reservation>();
            foreach (Reservation r in _search.SearchByUserId(idNumber.Trim()))
            {
                if (r.Status == ReservationStatus.Active)
                    mine.Add(r);
            }
            return MergeSorter.Sort(mine, new ReservationDateTimeComparer());
        }
    }

    public OperationResult CancelReservation(int reservationId, string enteredIdNumber)
    {
        ProcessDueReservations();

        lock (_schedulingGate)
        {
            Reservation? r = _search.FindById(reservationId);
            if (r == null)
                return new OperationResult(false, "Reservation not found.");

            if (!r.MatchesUser(enteredIdNumber ?? string.Empty))
                return new OperationResult(false,
                    "That ID number does not match the person who made this reservation.");

            if (r.Status != ReservationStatus.Active)
                return new OperationResult(false, "Only active reservations can be cancelled.");

            if (!_search.Cancel(reservationId))
                return new OperationResult(false, "The reservation could not be cancelled.");

            return new OperationResult(true,
                $"Reservation #{reservationId} was cancelled. {r.SpotOrRoom} is available again.");
        }
    }

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
