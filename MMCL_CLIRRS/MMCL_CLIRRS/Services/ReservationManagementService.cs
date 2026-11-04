namespace MMCL_CLIRRS;

public class ReservationManagementService
{
    private readonly ReservationLinkedList _reservations;
    private readonly ReservationSearchService _search;
    private readonly LinkedQueue<ReservationRequest> _requestQueue = new();
    private int _nextId;

    public ReservationManagementService(ReservationLinkedList reservations,
                                        ReservationSearchService search)
    {
        _reservations = reservations;
        _search = search;

        _nextId = _reservations.GetAll()
                               .Select(r => r.ReservationId)
                               .DefaultIfEmpty(1000)
                               .Max() + 1;
    }

    public OperationResult SubmitRequest(ReservationRequest request)
    {
        string? problem = Validate(request);
        if (problem != null)
            return new OperationResult(false, problem);

        _requestQueue.Enqueue(request);

        OperationResult result = new OperationResult(false, "Nothing was processed.");
        while (!_requestQueue.IsEmpty)
            result = ProcessNextRequest();

        return result;
    }

    private OperationResult ProcessNextRequest()
    {
        ReservationRequest request = _requestQueue.Dequeue();

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

        _reservations.Add(reservation);
        _search.Index(reservation);

        return new OperationResult(true,
            $"Reservation #{reservation.ReservationId} confirmed!\n\n" +
            $"{reservation.SpotOrRoom}\n" +
            $"{reservation.Date:ddd, dd MMM yyyy}, {Clock(reservation.StartTime)} - {Clock(reservation.EndTime)}");
    }

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

        DateTime today = DateTime.Today;
        if (r.UserType == UserType.Student && r.Date.Date != today)
            return "Students can only reserve for today.";
        if (r.Date.Date < today)
            return "You cannot reserve a date that has already passed.";

        if (r.Date.Date == today && r.StartTime < DateTime.Now.TimeOfDay)
            return "That start time has already passed. Please choose a later time.";

        return null;
    }

    public bool IsAvailable(string spotOrRoom, DateTime date, TimeSpan start, TimeSpan end)
        => FindConflict(spotOrRoom, date, start, end) == null;

    public List<Reservation> GetBookedSlots(string spotOrRoom, DateTime date)
    {
        var booked = new List<Reservation>();
        foreach (Reservation r in _search.SearchBySpot(spotOrRoom))
        {
            if (IsHoldingSpot(r) && r.Date.Date == date.Date)
                booked.Add(r);
        }
        return MergeSorter.Sort(booked, new ReservationDateTimeComparer());
    }

    private Reservation? FindConflict(string spotOrRoom, DateTime date, TimeSpan start, TimeSpan end)
    {
        foreach (Reservation other in GetBookedSlots(spotOrRoom, date))
        {
            if (start < other.EndTime && other.StartTime < end)
                return other;
        }
        return null;
    }

    private static bool IsHoldingSpot(Reservation r)
        => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Occupied;

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

        if (!_search.Cancel(reservationId))
            return new OperationResult(false, "The reservation could not be cancelled.");

        return new OperationResult(true,
            $"Reservation #{reservationId} was cancelled. {r.SpotOrRoom} is available again.");
    }

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
