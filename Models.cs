namespace MMCL_CLIRRS;

// ===== SHARED GROUP CONTRACT - DO NOT MODIFY WITHOUT TELLING EVERYONE =====

public enum ReservationStatus { Active, Completed, Cancelled, Expired }

public enum UserType { Student, Faculty }

public class Reservation
{
    public int ReservationId { get; }
    public string UserName { get; }
    public string UserIdNumber { get; }      // student/faculty ID
    public UserType UserType { get; }
    public string Department { get; }
    public string Program { get; }
    public int YearLevel { get; }
    public string SpotOrRoom { get; }        // e.g. "Table 3 - Seat 2" or "Discussion Room 1"
    public bool IsDiscussionRoom { get; }
    public DateTime Date { get; }
    public TimeSpan StartTime { get; }
    public ReservationStatus Status { get; private set; }

    public Reservation(int id, string userName, string userIdNumber, UserType userType,
                       string department, string program, int yearLevel,
                       string spotOrRoom, bool isDiscussionRoom,
                       DateTime date, TimeSpan startTime)
    {
        ReservationId = id;
        UserName = userName;
        UserIdNumber = userIdNumber;
        UserType = userType;
        Department = department;
        Program = program;
        YearLevel = yearLevel;
        SpotOrRoom = spotOrRoom;
        IsDiscussionRoom = isDiscussionRoom;
        Date = date;
        StartTime = startTime;
        Status = ReservationStatus.Active;
    }

    public void Cancel() => Status = ReservationStatus.Cancelled;
    public void MarkExpired() => Status = ReservationStatus.Expired;
    public void MarkCompleted() => Status = ReservationStatus.Completed;
}

public interface IReservationRepository
{
    IEnumerable<Reservation> GetAll();
    Reservation? GetById(int reservationId);
    void Add(Reservation reservation);
}