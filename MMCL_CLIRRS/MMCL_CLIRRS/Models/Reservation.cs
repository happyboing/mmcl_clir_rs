namespace MMCL_CLIRRS;

public class Reservation
{
    public int ReservationId { get; }
    public string UserName { get; }
    public string UserIdNumber { get; }
    public UserType UserType { get; }
    public string Department { get; }
    public string Program { get; }
    public int YearLevel { get; }
    public string SpotOrRoom { get; private set; }
    public bool IsDiscussionRoom { get; private set; }
    public DateTime Date { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public ReservationStatus Status { get; private set; }

    public Reservation(int id, string userName, string userIdNumber, UserType userType,
                       string department, string program, int yearLevel,
                       string spotOrRoom, bool isDiscussionRoom,
                       DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after the start time.");

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
        EndTime = endTime;
        Status = ReservationStatus.Active;
    }

    public void Reschedule(string spotOrRoom, bool isDiscussionRoom,
                           DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        if (string.IsNullOrWhiteSpace(spotOrRoom))
            throw new ArgumentException("Spot or room is required.");
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after the start time.");

        SpotOrRoom = spotOrRoom.Trim();
        IsDiscussionRoom = isDiscussionRoom;
        Date = date.Date;
        StartTime = startTime;
        EndTime = endTime;
    }

    public bool MatchesUser(string idNumber)
        => string.Equals(UserIdNumber, idNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    public void Cancel() => Status = ReservationStatus.Cancelled;
    public void MarkExpired() => Status = ReservationStatus.Expired;
    public void MarkOccupied() => Status = ReservationStatus.Occupied;
    public void MarkCompleted() => Status = ReservationStatus.Completed;
    public void Reactivate() => Status = ReservationStatus.Active;
}