namespace MMCL_CLIRRS;

public class ReservationSearchService
{
    private readonly IReservationRepository _repository;

    private AvlTree<int> _byId = null!;
    private AvlTree<string> _byName = null!;
    private AvlTree<string> _byUserId = null!;
    private AvlTree<DateTime> _byDate = null!;
    private AvlTree<string> _bySpot = null!;
    private AvlTree<ReservationStatus> _byStatus = null!;

    public ReservationSearchService(IReservationRepository repository)
    {
        _repository = repository;
        Reload();
    }

    // Rebuild every index from the master collection.
    public void Reload()
    {
        _byId = new AvlTree<int>(r => r.ReservationId);
        _byName = new AvlTree<string>(r => r.UserName, StringComparer.OrdinalIgnoreCase);
        _byUserId = new AvlTree<string>(r => r.UserIdNumber, StringComparer.OrdinalIgnoreCase);
        _byDate = new AvlTree<DateTime>(r => r.Date.Date);
        _bySpot = new AvlTree<string>(r => r.SpotOrRoom, StringComparer.OrdinalIgnoreCase);
        _byStatus = new AvlTree<ReservationStatus>(r => r.Status);

        foreach (Reservation r in _repository.GetAll())
            Index(r);
    }

    // Functionality 1 should call this whenever a new reservation is created.
    public void Index(Reservation r)
    {
        _byId.Insert(r);
        _byName.Insert(r);
        _byUserId.Insert(r);
        _byDate.Insert(r);
        _bySpot.Insert(r);
        _byStatus.Insert(r);
    }

    // ---------- Searches ----------
    public Reservation? FindById(int id) => _byId.Find(id).FirstOrDefault();
    public List<Reservation> SearchByName(string name) => _byName.Find(name).ToList();
    public List<Reservation> SearchByUserId(string userId) => _byUserId.Find(userId).ToList();
    public List<Reservation> SearchBySpot(string spot) => _bySpot.Find(spot).ToList();
    public List<Reservation> SearchByStatus(ReservationStatus status) => _byStatus.Find(status).ToList();

    public List<Reservation> SearchByDateRange(DateTime from, DateTime to)
        => _byDate.FindRange(from.Date, to.Date);

    public List<Reservation> GetDiscussionRooms()
        => _repository.GetAll().Where(r => r.IsDiscussionRoom).ToList();

    // Chronological list using your Merge Sort.
    public List<Reservation> GetAllChronological()
        => MergeSorter.Sort(_repository.GetAll().ToList(), new ReservationDateTimeComparer());

    // ---------- Management ----------
    public bool Cancel(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Active, r => r.Cancel());

    // For Functionality 2 to call when the grace period or end time passes.
    public bool MarkExpired(int reservationId)
        => ChangeStatus(reservationId, ReservationStatus.Active, r => r.MarkExpired());

    // Remove from the status tree BEFORE the key changes, then re-insert.
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