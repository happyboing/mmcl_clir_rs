namespace MMCL_CLIRRS;

public class ReservationSearchService
{
    private readonly IReservationRepository _repository;

    private HashIndex<int> _byId = null!;
    private HashIndex<string> _byUserId = null!;
    private HashIndex<string> _bySpot = null!;
    private HashIndex<ReservationStatus> _byStatus = null!;
    private HashIndex<bool> _byRoomType = null!;

    private AvlTree<string> _byName = null!;
    private AvlTree<DateTime> _byDate = null!;

    private List<IReservationIndex> _indexes = null!;

    private readonly LinkedStack<ActivityLogEntry> _activityLog = new LinkedStack<ActivityLogEntry>();
    private readonly LinkedStack<UndoableOperation> _undoStack = new LinkedStack<UndoableOperation>();

    public event Action<Reservation>? ReservationChanged;

    public ReservationSearchService(IReservationRepository repository)
    {
        _repository = repository;
        Reload();
    }

    public void Reload()
    {
        _byId = new HashIndex<int>(r => r.ReservationId);
        _byUserId = new HashIndex<string>(r => r.UserIdNumber, StringComparer.OrdinalIgnoreCase);
        _bySpot = new HashIndex<string>(r => r.SpotOrRoom, StringComparer.OrdinalIgnoreCase);
        _byStatus = new HashIndex<ReservationStatus>(r => r.Status);
        _byRoomType = new HashIndex<bool>(r => r.IsDiscussionRoom);

        _byName = new AvlTree<string>(r => r.UserName, StringComparer.OrdinalIgnoreCase);
        _byDate = new AvlTree<DateTime>(r => r.Date.Date);

        _indexes = new List<IReservationIndex>
            { _byId, _byUserId, _bySpot, _byStatus, _byRoomType, _byName, _byDate };

        foreach (Reservation r in _repository.GetAll())
            Index(r);
    }

    public void Index(Reservation r)
    {
        foreach (IReservationIndex index in _indexes)
            index.Insert(r);

        ReservationChanged?.Invoke(r);
    }

    public Reservation? FindById(int id) => _byId.Find(id).FirstOrDefault();
    public List<Reservation> SearchByUserId(string userId) => _byUserId.Find(userId).ToList();
    public List<Reservation> SearchBySpot(string spot) => _bySpot.Find(spot).ToList();
    public List<Reservation> SearchByStatus(ReservationStatus status) => _byStatus.Find(status).ToList();
    public List<Reservation> GetDiscussionRooms() => _byRoomType.Find(true).ToList();

    public List<Reservation> SearchByName(string prefix)
        => _byName.FindRange(prefix, prefix + '\uffff');

    public List<Reservation> SearchByDateRange(DateTime from, DateTime to)
        => _byDate.FindRange(from.Date, to.Date);

    public List<Reservation> GetAllChronological()
        => MergeSorter.Sort(_repository.GetAll().ToList(), new ReservationDateTimeComparer());

    public bool Cancel(int reservationId, bool undoable = false)
    {
        Reservation? r = ChangeStatus(reservationId, ReservationStatus.Active, x => x.Cancel(),
                                      OperationType.Cancel, "Reservation cancelled");
        if (r == null) return false;

        if (undoable)
            _undoStack.Push(new ReactivateOperation(r,
                $"Cancelled reservation #{r.ReservationId} ({r.UserName})"));
        return true;
    }

    public bool MarkExpired(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Active, x => x.MarkExpired(),
                        OperationType.Expire, "Not occupied within the grace period") != null;

    public bool MarkCompleted(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Occupied, x => x.MarkCompleted(),
                        OperationType.Complete, "Reservation time ended") != null;

    public OperationResult Verify(int reservationId, string enteredIdNumber)
    {
        Reservation? r = FindById(reservationId);
        if (r == null) return new OperationResult(false, "Reservation not found.");
        if (r.Status != ReservationStatus.Active)
            return new OperationResult(false, "Only active reservations can be verified.");

        if (!r.MatchesUser(enteredIdNumber))
        {
            Log(OperationType.VerifyFailed, r, "ID number did not match the reservation");
            return new OperationResult(false,
                "That ID number does not match the person who reserved this spot.");
        }

        ApplyChange(r, x => x.MarkOccupied());
        Log(OperationType.Verify, r, "Occupant verified");
        _undoStack.Push(new ReactivateOperation(r, $"Verified {r.UserName} (#{r.ReservationId})"));
        return new OperationResult(true, $"{r.UserName} is verified. The reservation is now Occupied.");
    }

    public OperationResult Update(int reservationId, string spotOrRoom,
                                  DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        Reservation? r = FindById(reservationId);
        if (r == null) return new OperationResult(false, "Reservation not found.");
        if (r.Status != ReservationStatus.Active)
            return new OperationResult(false, "Only active reservations can be updated.");

        string spot = spotOrRoom.Trim();
        if (!ClirLayout.Exists(spot))
            return new OperationResult(false, "Please choose a seat or room that exists in the CLIR layout.");
        if (endTime <= startTime)
            return new OperationResult(false, "End time must be after the start time.");

        string? pastProblem = CheckNotInThePast(r, date.Date, startTime, endTime);
        if (pastProblem != null) return new OperationResult(false, pastProblem);

        Reservation? conflict = FindConflict(r, spot, date.Date, startTime, endTime);
        if (conflict != null)
            return new OperationResult(false,
                $"That slot overlaps reservation #{conflict.ReservationId} ({conflict.UserName}, " +
                $"{Clock(conflict.StartTime)}-{Clock(conflict.EndTime)}) for the same spot.");

        bool isDiscussionRoom = ClirLayout.GetDiscussionRooms().Contains(spot);

        string before = Describe(r);
        var undo = new RescheduleOperation(r,
            $"Updated reservation #{r.ReservationId} ({r.UserName})");

        ApplyChange(r, x => x.Reschedule(spot, isDiscussionRoom, date, startTime, endTime));
        Log(OperationType.Update, r, $"{before}  ->  {Describe(r)}");
        _undoStack.Push(undo);
        return new OperationResult(true, "Reservation updated.");
    }

    public string? NextUndoDescription
        => _undoStack.IsEmpty ? null : _undoStack.Peek().Description;

    public OperationResult UndoLast()
    {
        if (_undoStack.IsEmpty)
            return new OperationResult(false, "There is nothing to undo.");

        UndoableOperation operation = _undoStack.Peek();
        Reservation r = operation.Reservation;

        if (!operation.StillApplies)
        {
            _undoStack.Pop();
            return new OperationResult(false,
                $"Can't undo: reservation #{r.ReservationId} is now {r.Status}, so this action no longer applies.");
        }

        var slot = operation.SlotAfterUndo;
        Reservation? conflict = FindConflict(r, slot.Spot, slot.Date.Date, slot.Start, slot.End);
        if (conflict != null)
            return new OperationResult(false,
                $"Can't undo: the slot was taken by reservation #{conflict.ReservationId} ({conflict.UserName}).");

        _undoStack.Pop();
        ApplyChange(r, _ => operation.Revert());
        Log(OperationType.Undo, r, $"Undid: {operation.Description}");
        return new OperationResult(true, $"Undone: {operation.Description}");
    }

    public List<ActivityLogEntry> GetRecentActivity(int count = 100)
        => _activityLog.ToList().Take(count).ToList();

    private Reservation? ChangeStatus(int id, ReservationStatus mustBe, Action<Reservation> change,
                                      OperationType operation, string details)
    {
        Reservation? r = FindById(id);
        if (r == null || r.Status != mustBe) return null;

        ApplyChange(r, change);
        Log(operation, r, details);
        return r;
    }

    private void ApplyChange(Reservation r, Action<Reservation> change)
    {
        foreach (IReservationIndex index in _indexes)
            index.Remove(r);

        try { change(r); }
        finally
        {
            foreach (IReservationIndex index in _indexes)
                index.Insert(r);
        }

        ReservationChanged?.Invoke(r);
    }

    private Reservation? FindConflict(Reservation self, string spot, DateTime date,
                                      TimeSpan start, TimeSpan end)
    {
        foreach (Reservation other in _bySpot.Find(spot))
        {
            if (ReferenceEquals(other, self)) continue;
            if (other.Status != ReservationStatus.Active &&
                other.Status != ReservationStatus.Occupied) continue;
            if (other.Date.Date != date) continue;

            if (start < other.EndTime && other.StartTime < end)
                return other;
        }
        return null;
    }
    private static string? CheckNotInThePast(Reservation r, DateTime date, TimeSpan start, TimeSpan end)
    {
        DateTime now = DateTime.Now;

        if (date < now.Date)
            return "That date has already passed.";

        if (date == now.Date)
        {
            if (end <= now.TimeOfDay)
                return "That end time has already passed.";

            bool startMoved = date != r.Date.Date || start != r.StartTime;
            if (startMoved && start < now.TimeOfDay)
                return "That start time has already passed. Please choose a later time.";
        }
        return null;
    }
    private void Log(OperationType operation, Reservation r, string details)
        => _activityLog.Push(new ActivityLogEntry(DateTime.Now, operation, r.ReservationId, r.UserName, details));

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");

    private static string Describe(Reservation r)
        => $"{r.SpotOrRoom}, {r.Date:dd MMM}, {Clock(r.StartTime)}-{Clock(r.EndTime)}";
}