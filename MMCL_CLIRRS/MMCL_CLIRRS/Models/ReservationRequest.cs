namespace MMCL_CLIRRS;

public class ReservationRequest
{
    public string UserName { get; init; } = "";
    public string UserIdNumber { get; init; } = "";
    public UserType UserType { get; init; }
    public string Department { get; init; } = "";
    public string Program { get; init; } = "";
    public int YearLevel { get; init; }
    public string SpotOrRoom { get; init; } = "";
    public bool IsDiscussionRoom { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    public DateTime SubmittedAt { get; } = DateTime.Now;
}
