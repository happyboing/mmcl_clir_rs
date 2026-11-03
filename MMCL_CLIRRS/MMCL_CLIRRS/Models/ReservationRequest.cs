namespace MMCL_CLIRRS;

// What the user typed into the form. It waits in the QUEUE until the
// system turns it into a real Reservation (or rejects it).
public class ReservationRequest
{
    public string UserName { get; init; } = "";
    public string UserIdNumber { get; init; } = "";      // student / faculty ID
    public UserType UserType { get; init; }
    public string Department { get; init; } = "";
    public string Program { get; init; } = "";
    public int YearLevel { get; init; }                   // 0 for faculty
    public string SpotOrRoom { get; init; } = "";
    public bool IsDiscussionRoom { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    // The moment the request was submitted (the queue keeps them in this order).
    public DateTime SubmittedAt { get; } = DateTime.Now;
}
