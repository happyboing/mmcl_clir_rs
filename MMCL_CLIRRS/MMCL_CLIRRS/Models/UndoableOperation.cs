namespace MMCL_CLIRRS;

public abstract class UndoableOperation
{
    public Reservation Reservation { get; }
    public string Description { get; }

    public ReservationStatus ExpectedStatus { get; }

    protected UndoableOperation(Reservation reservation, string description)
    {
        Reservation = reservation;
        Description = description;
        ExpectedStatus = reservation.Status;
    }

    public bool StillApplies => Reservation.Status == ExpectedStatus;

    public virtual (string Spot, DateTime Date, TimeSpan Start, TimeSpan End) SlotAfterUndo
        => (Reservation.SpotOrRoom, Reservation.Date, Reservation.StartTime, Reservation.EndTime);

    public abstract void Revert();
}

public class ReactivateOperation : UndoableOperation
{
    public ReactivateOperation(Reservation reservation, string description)
        : base(reservation, description) { }

    public override void Revert() => Reservation.Reactivate();
}

public class RescheduleOperation : UndoableOperation
{
    private readonly string _oldSpot;
    private readonly bool _oldIsDiscussionRoom;
    private readonly DateTime _oldDate;
    private readonly TimeSpan _oldStart;
    private readonly TimeSpan _oldEnd;

    public RescheduleOperation(Reservation reservation, string description)
        : base(reservation, description)
    {
        _oldSpot = reservation.SpotOrRoom;
        _oldIsDiscussionRoom = reservation.IsDiscussionRoom;
        _oldDate = reservation.Date;
        _oldStart = reservation.StartTime;
        _oldEnd = reservation.EndTime;
    }

    public override (string Spot, DateTime Date, TimeSpan Start, TimeSpan End) SlotAfterUndo
        => (_oldSpot, _oldDate, _oldStart, _oldEnd);

    public override void Revert()
        => Reservation.Reschedule(_oldSpot, _oldIsDiscussionRoom, _oldDate, _oldStart, _oldEnd);
}