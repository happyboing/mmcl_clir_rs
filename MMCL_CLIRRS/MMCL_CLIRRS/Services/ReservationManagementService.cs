namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #1: Seat and Room Reservation Management
//  This class holds the BRAINS (the rules). The forms only show
//  buttons and text boxes and call the methods below.
// ============================================================
public class ReservationManagementService
{
    private readonly ReservationLinkedList _reservations;                    // LINKED LIST: master storage
    private readonly ReservationSearchService _search;                       // Functionality #4: keeps the search indexes
    private readonly LinkedQueue<ReservationRequest> _requestQueue = new();  // QUEUE: first-come, first-served
    private int _nextId;

    public ReservationManagementService(ReservationLinkedList reservations,
                                        ReservationSearchService search)
    {
        _reservations = reservations;
        _search = search;

        // New reservation IDs continue after the biggest existing ID.
        _nextId = _reservations.GetAll()
                               .Select(r => r.ReservationId)
                               .DefaultIfEmpty(1000)
                               .Max() + 1;
    }

    // =========================================================
    //  MAKING A RESERVATION
    // =========================================================

    // Step 1: check the rules.  Step 2: join the queue.  Step 3: the queue is served in order.
    public OperationResult SubmitRequest(ReservationRequest request)
    {
        string? problem = Validate(request);
        if (problem != null)
            return new OperationResult(false, problem);

        _requestQueue.Enqueue(request);

        // Serve everyone in line, front first. (With one kiosk the line is usually just 1 person,
        // but if requests ever arrive together, the one submitted first is always served first.)
        OperationResult result = new OperationResult(false, "Nothing was processed.");
        while (!_requestQueue.IsEmpty)
            result = ProcessNextRequest();

        return result;
    }

    // Take the FRONT request out of the queue and try to turn it into a real reservation.
    private OperationResult ProcessNextRequest()
    {
        ReservationRequest request = _requestQueue.Dequeue();

        // Check again at the moment it is this request's turn:
        // someone ahead in line may have just taken the same spot.
        Reservation? conflict = FindConflict(request.SpotOrRoom, request.Date,
                                             request.StartTime, request.EndTime);
        if (conflict != null)
        {
            return new OperationResult(false,
                $"{request.SpotOrRoom} is already reserved from {Clock(conflict.StartTime)} " +
                $"to {Clock(conflict.EndTime)} on that date. Please pick another time or spot.");
        }

        var reservation = new Reservation(
            _nextId++, request.UserName, request.UserIdNumber, request.UserType,
            request.Department, request.Program, request.YearLevel,
            request.SpotOrRoom, request.IsDiscussionRoom,
            request.Date, request.StartTime, request.EndTime);

        _reservations.Add(reservation);   // store it in the linked list
        _search.Index(reservation);       // tell the search system (#4) about it

        return new OperationResult(true,
            $"Reservation #{reservation.ReservationId} confirmed!\n\n" +
            $"{reservation.SpotOrRoom}\n" +
            $"{reservation.Date:ddd, dd MMM yyyy}, {Clock(reservation.StartTime)} - {Clock(reservation.EndTime)}");
    }

    // Returns null when everything is fine, otherwise the reason it is NOT fine.
    private string? Validate(ReservationRequest r)
    {
        if (r.UserName.Length == 0) return "Please enter your name.";
        if (r.UserIdNumber.Length == 0) return "Please enter your student / faculty ID number.";
        if (r.Department.Length == 0) return "Please enter your department.";
        if (r.Program.Length == 0) return "Please enter your program.";
        if (r.UserType == UserType.Student && (r.YearLevel < 1 || r.YearLevel > 6))
            return "Please enter a valid year level.";

        if (r.SpotOrRoom.Length == 0 || !ClirLayout.Exists(r.SpotOrRoom))
            return "Please choose a seat or room from the list.";
        if (r.EndTime <= r.StartTime)
            return "The end time must be after the start time.";

        // Rule from the proposal: students = today only, faculty = today or upcoming dates.
        DateTime today = DateTime.Today;
        if (r.UserType == UserType.Student && r.Date.Date != today)
            return "Students can only reserve for today.";
        if (r.Date.Date < today)
            return "You cannot reserve a date that has already passed.";

        if (r.Date.Date == today && r.StartTime < DateTime.Now.TimeOfDay)
            return "That start time has already passed. Please choose a later time.";

        return null;
    }

    // =========================================================
    //  AVAILABILITY  (prevents double booking)
    // =========================================================

    public bool IsAvailable(string spotOrRoom, DateTime date, TimeSpan start, TimeSpan end)
        => FindConflict(spotOrRoom, date, start, end) == null;

    // Every reservation for this spot on this date that is still holding it, earliest first.
    public List<Reservation> GetBookedSlots(string spotOrRoom, DateTime date)
    {
        var booked = new List<Reservation>();
        foreach (Reservation r in _search.SearchBySpot(spotOrRoom))
        {
            if (IsHoldingSpot(r) && r.Date.Date == date.Date)
                booked.Add(r);
        }
        return MergeSorter.Sort(booked, new ReservationDateTimeComparer());   // Merge Sort by time
    }

    // Two time ranges overlap when each one starts before the other one ends.
    private Reservation? FindConflict(string spotOrRoom, DateTime date, TimeSpan start, TimeSpan end)
    {
        foreach (Reservation other in GetBookedSlots(spotOrRoom, date))
        {
            if (start < other.EndTime && other.StartTime < end)
                return other;
        }
        return null;
    }

    // Cancelled / Expired / Completed reservations free the spot. Active and Occupied do not.
    private static bool IsHoldingSpot(Reservation r)
        => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Occupied;

    // =========================================================
    //  CANCELLING (by student number)
    // =========================================================

    // The active reservations that belong to this ID number.
    public List<Reservation> GetActiveReservationsOf(string idNumber)
    {
        var mine = new List<Reservation>();
        foreach (Reservation r in _search.SearchByUserId(idNumber.Trim()))
        {
            if (r.Status == ReservationStatus.Active)
                mine.Add(r);
        }
        return MergeSorter.Sort(mine, new ReservationDateTimeComparer());
    }

    // Verify that the entered number really belongs to this reservation, then cancel it.
    public OperationResult CancelReservation(int reservationId, string enteredIdNumber)
    {
        Reservation? r = _search.FindById(reservationId);
        if (r == null)
            return new OperationResult(false, "Reservation not found.");

        if (!r.MatchesUser(enteredIdNumber))
            return new OperationResult(false,
                "That ID number does not match the person who made this reservation.");

        if (r.Status != ReservationStatus.Active)
            return new OperationResult(false, "Only active reservations can be cancelled.");

        // Functionality #4 changes the status to Cancelled and writes it in the activity log.
        // Because Cancelled no longer "holds" the spot, it is automatically available again.
        if (!_search.Cancel(reservationId))
            return new OperationResult(false, "The reservation could not be cancelled.");

        return new OperationResult(true,
            $"Reservation #{reservationId} was cancelled. {r.SpotOrRoom} is available again.");
    }

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
