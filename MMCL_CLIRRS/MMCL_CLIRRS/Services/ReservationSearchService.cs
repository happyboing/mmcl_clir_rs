namespace MMCL_CLIRRS;

public class ReservationSearchService
{
    private readonly IReservationRepository _repository;

    // Hash tables: exact-match lookups, O(1) on average.
    private HashIndex<int> _byId = null!;
    private HashIndex<string> _byUserId = null!;
    private HashIndex<string> _bySpot = null!;
    private HashIndex<ReservationStatus> _byStatus = null!;
    private HashIndex<bool> _byRoomType = null!;

    // AVL trees: ordered searches (prefix and range), O(log n).
    private AvlTree<string> _byName = null!;
    private AvlTree<DateTime> _byDate = null!;

    private List<IReservationIndex> _indexes = null!;

    public ReservationSearchService(IReservationRepository repository)
    {
        _repository = repository;
        Reload();
    }

    // Rebuild every index from the master collection.
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

    // Functionality 1 should call this whenever a new reservation is created.
    public void Index(Reservation r)
    {
        foreach (IReservationIndex index in _indexes)
            index.Insert(r);
    }

    // ---------- Searches ----------
    public Reservation? FindById(int id) => _byId.Find(id).FirstOrDefault();
    public List<Reservation> SearchByUserId(string userId) => _byUserId.Find(userId).ToList();
    public List<Reservation> SearchBySpot(string spot) => _bySpot.Find(spot).ToList();
    public List<Reservation> SearchByStatus(ReservationStatus status) => _byStatus.Find(status).ToList();
    public List<Reservation> GetDiscussionRooms() => _byRoomType.Find(true).ToList();

    // Prefix search: "mar" finds Maria Santos. Works as a range query on the sorted name tree.
    public List<Reservation> SearchByName(string prefix)
        => _byName.FindRange(prefix, prefix + '\uffff');

    public List<Reservation> SearchByDateRange(DateTime from, DateTime to)
        => _byDate.FindRange(from.Date, to.Date);

    // Chronological list using your Merge Sort.
    public List<Reservation> GetAllChronological()
        => MergeSorter.Sort(_repository.GetAll().ToList(), new ReservationDateTimeComparer());

    // ---------- Management ----------
    public bool Cancel(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Active, r => r.Cancel());

    // For Functionality 2 to call when the grace period or end time passes.
    public bool MarkExpired(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Active, r => r.MarkExpired());

    // Remove from the status index BEFORE the key changes, then re-insert.
    private bool ChangeStatus(int id, ReservationStatus mustBe, Action<Reservation> change)
    {
        Reservation? r = FindById(id);
        if (r == null || r.Status != mustBe) return false;

        _byStatus.Remove(r);
        change(r);
        _byStatus.Insert(r);
        return true;
    }
}