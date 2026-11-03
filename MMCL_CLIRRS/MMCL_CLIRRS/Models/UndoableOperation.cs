namespace MMCL_CLIRRS;

// One staff action that can be reversed. Each subclass knows how to undo itself.
public abstract class UndoableOperation
{
    public Reservation Reservation { get; }
    public string Description { get; }

    // The status the reservation had right after the action. If it differs later,
    // something else changed the reservation and this undo no longer applies.
    public ReservationStatus ExpectedStatus { get; }

    protected UndoableOperation(Reservation reservation, string description)
    {
        Reservation = reservation;
        Description = description;
        ExpectedStatus = reservation.Status;
    }

    public bool StillApplies => Reservation.Status == ExpectedStatus;

    // The slot the reservation will hold again once the undo is done.
    public virtual (string Spot, DateTime Date, TimeSpan Start, TimeSpan End) SlotAfterUndo
        => (Reservation.SpotOrRoom, Reservation.Date, Reservation.StartTime, Reservation.EndTime);

    public abstract void Revert();
}

// Undoes a cancellation or a verification: the reservation goes back to Active.
// Create it AFTER the action, so ExpectedStatus is the new status.
public class ReactivateOperation : UndoableOperation
{
    public ReactivateOperation(Reservation reservation, string description)
        : base(reservation, description) { }

    public override void Revert() => Reservation.Reactivate();
}

// Undoes an update by restoring the old spot, date, and times.
// Create it BEFORE the update, so it captures the old values.
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