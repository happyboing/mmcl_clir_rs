namespace MMCL_CLIRRS;

// The current status of one spot, plus the reservation that is holding it.
public sealed record SpotSnapshot(SpotAvailability Availability, Reservation? Reservation);

// ============================================================
//  Functionality #3: CLIR Interactive Seat Map
//  Reads the availability that Functionality #1 already computes.
//  A spot is:
//    Occupied - a verified reservation covers the selected time
//    Reserved - an active reservation covers the selected time
//    Available - no active or occupied reservation covers it
// ============================================================
public class SeatMapService
{
    private readonly ReservationManagementService _management;

    public SeatMapService(ReservationManagementService management)
    {
        _management = management;
    }

    public SpotSnapshot GetSnapshot(ClirMapSpot spot, DateTime date, TimeSpan time)
    {
        Reservation? covering = null;

        foreach (Reservation reservation in _management.GetBookedSlots(spot.SpotOrRoom, date))
        {
            if (reservation.StartTime <= time && time < reservation.EndTime)
            {
                covering = reservation;

                // If the data ever contains overlapping holds, an occupied
                // reservation wins over an active one.
                if (reservation.Status == ReservationStatus.Occupied)
                    break;
            }
        }

        if (covering == null)
            return new SpotSnapshot(SpotAvailability.Available, null);

        SpotAvailability availability = covering.Status == ReservationStatus.Occupied
            ? SpotAvailability.Occupied
            : SpotAvailability.Reserved;

        return new SpotSnapshot(availability, covering);
    }

    // Every reservation that still holds this spot on the chosen date,
    // earliest first. Used by the details panel.
    public List<Reservation> GetDaySchedule(string spotOrRoom, DateTime date)
        => _management.GetBookedSlots(spotOrRoom, date);

    // The terminal asks for the user's ID number and the existing
    // cancellation rules verify that the number owns the reservation.
    public OperationResult Cancel(int reservationId, string enteredIdNumber)
        => _management.CancelReservation(reservationId, enteredIdNumber);
}
