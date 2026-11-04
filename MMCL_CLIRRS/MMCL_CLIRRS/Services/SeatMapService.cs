namespace MMCL_CLIRRS;

public sealed record SpotSnapshot(SpotAvailability Availability, Reservation? Reservation);

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

    public List<Reservation> GetDaySchedule(string spotOrRoom, DateTime date)
        => _management.GetBookedSlots(spotOrRoom, date);

    public OperationResult Cancel(int reservationId, string enteredIdNumber)
        => _management.CancelReservation(reservationId, enteredIdNumber);
}
