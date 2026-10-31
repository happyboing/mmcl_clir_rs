namespace MMCL_CLIRRS;

/// TEMPORARY repository so Functionality 1 can run on its own.
/// Backed by the built-in LinkedList. Swap with the group's real
/// IReservationRepository implementation when it's ready - nothing else changes.
public class InMemoryReservationRepository : IReservationRepository
{
    private readonly LinkedList<Reservation> _records = new();

    public IEnumerable<Reservation> GetAll() => _records;

    public Reservation? GetById(int reservationId) =>
        _records.FirstOrDefault(r => r.ReservationId == reservationId);

    public void Add(Reservation reservation) => _records.AddLast(reservation);
}


/// Functionality 1 logic: reserve, double-booking prevention, cancel with ID verification.
/// Talks ONLY to IReservationRepository.
public class ReservationService
{
    private readonly IReservationRepository _repo;

    public ReservationService(IReservationRepository repo) => _repo = repo;

    /// The active reservation on this spot/date/time, or null if the spot is free.</summary>
    public Reservation? GetActiveReservation(string spotOrRoom, DateTime date, TimeSpan startTime) =>
        _repo.GetAll().FirstOrDefault(r =>
            r.Status == ReservationStatus.Active &&
            r.SpotOrRoom == spotOrRoom &&
            r.Date.Date == date.Date &&
            r.StartTime == startTime);

    public bool IsAvailable(string spotOrRoom, DateTime date, TimeSpan startTime) =>
        GetActiveReservation(spotOrRoom, date, startTime) == null;

    public (bool Success, string Message) TryReserve(
        string userName, string userIdNumber, UserType userType,
        string department, string program, int yearLevel,
        string spotOrRoom, bool isDiscussionRoom,
        DateTime date, TimeSpan startTime)
    {
        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(userIdNumber) ||
            string.IsNullOrWhiteSpace(department) ||
            string.IsNullOrWhiteSpace(program))
            return (false, "Please fill in all user details.");

        if (string.IsNullOrWhiteSpace(spotOrRoom))
            return (false, "Please select a seat or room from the map.");

        // Prevent multiple users reserving the same spot/room for the same time
        if (!IsAvailable(spotOrRoom, date, startTime))
            return (false, $"{spotOrRoom} is already reserved for that time.");

        int newId = _repo.GetAll().Select(r => r.ReservationId).DefaultIfEmpty(0).Max() + 1;

        var reservation = new Reservation(newId, userName.Trim(), userIdNumber.Trim(), userType,
            department.Trim(), program.Trim(), yearLevel,
            spotOrRoom, isDiscussionRoom, date.Date, startTime);

        _repo.Add(reservation);
        return (true, $"Reservation #{newId} confirmed for {spotOrRoom}.");
    }

    /// Cancels the active reservation on the spot only if the entered ID matches
    /// the ID that made the reservation. Status becomes Cancelled, so the spot
    /// is automatically available again.
    public (bool Success, string Message) TryCancel(
        string spotOrRoom, DateTime date, TimeSpan startTime, string enteredIdNumber)
    {
        var reservation = GetActiveReservation(spotOrRoom, date, startTime);
        if (reservation == null)
            return (false, "There is no active reservation on this spot.");

        if (!string.Equals(reservation.UserIdNumber, enteredIdNumber?.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "The ID number does not match the one on this reservation.");

        reservation.Cancel();
        return (true, $"Reservation #{reservation.ReservationId} cancelled. {spotOrRoom} is available again.");
    }
}