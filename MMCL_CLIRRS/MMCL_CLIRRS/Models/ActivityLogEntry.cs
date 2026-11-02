namespace MMCL_CLIRRS;

public enum OperationType { Cancel, Expire, Complete, Update, Verify, VerifyFailed }

public class ActivityLogEntry
{
    public DateTime Timestamp { get; }
    public OperationType Operation { get; }
    public int ReservationId { get; }
    public string UserName { get; }
    public string Details { get; }

    public ActivityLogEntry(DateTime timestamp, OperationType operation,
                            int reservationId, string userName, string details)
    {
        Timestamp = timestamp;
        Operation = operation;
        ReservationId = reservationId;
        UserName = userName;
        Details = details;
    }

    public override string ToString()
        => $"{Timestamp:dd MMM HH:mm:ss}  {Operation,-12} #{ReservationId}  {UserName}  |  {Details}";
}