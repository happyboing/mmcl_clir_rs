namespace MMCL_CLIRRS;

// A staff-facing notification produced when a reservation is not occupied
// within its five-minute grace period.
public sealed class ReservationExpirationNotice
{
    public int ReservationId { get; }
    public string UserName { get; }
    public string SpotOrRoom { get; }
    public DateTime ScheduledStart { get; }
    public DateTime ExpiredAt { get; }

    public ReservationExpirationNotice(int reservationId, string userName,
                                       string spotOrRoom, DateTime scheduledStart,
                                       DateTime expiredAt)
    {
        ReservationId = reservationId;
        UserName = userName;
        SpotOrRoom = spotOrRoom;
        ScheduledStart = scheduledStart;
        ExpiredAt = expiredAt;
    }
}
