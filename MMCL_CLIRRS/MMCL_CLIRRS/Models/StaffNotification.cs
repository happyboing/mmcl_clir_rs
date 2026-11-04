namespace MMCL_CLIRRS;

public sealed record StaffNotification(
    DateTime RaisedAt,
    int ReservationId,
    string UserName,
    string UserIdNumber,
    string SpotOrRoom,
    DateTime ReservedFor)
{
    public string Message
        => $"{SpotOrRoom} was not occupied within {SchedulingRules.GracePeriod.TotalMinutes:0} minutes " +
           $"of {ReservedFor:dd MMM HH:mm}. Reservation #{ReservationId} ({UserName}, {UserIdNumber}) " +
           "was released.";

    public override string ToString() => $"{RaisedAt:HH:mm:ss}   {Message}";
}
