namespace MMCL_CLIRRS;

public class MockReservationRepository : IReservationRepository
{
    private readonly List<Reservation> _reservations = new List<Reservation>();

    public MockReservationRepository()
    {
        string[] names =
        {
            "Juan Dela Cruz", "Maria Santos", "Pedro Reyes", "Ana Garcia",
            "Jose Ramos", "Liza Mendoza", "Carlo Bautista", "Bea Navarro"
        };
        string[] spots =
        {
            "Table 1 - Seat 1", "Table 1 - Seat 2", "Table 2 - Seat 1",
            "Table 3 - Seat 4", "Discussion Room 1", "Couch 1"
        };

        for (int i = 0; i < 20; i++)
        {
            string spot = spots[i % spots.Length];
            int startHour = 8 + (i % 9);

            var reservation = new Reservation(
                id: 1001 + i,
                userName: names[i % names.Length],
                userIdNumber: $"2023-{100000 + i}",
                userType: i % 6 == 0 ? UserType.Faculty : UserType.Student,
                department: "CCIS",
                program: "IT",
                yearLevel: 1 + (i % 4),
                spotOrRoom: spot,
                isDiscussionRoom: spot.StartsWith("Discussion"),
                date: DateTime.Today.AddDays(i % 7),
                startTime: TimeSpan.FromHours(startHour),
                endTime: TimeSpan.FromHours(startHour + 1));

            if (i % 5 == 0) reservation.Cancel();
            else if (i % 7 == 0) reservation.MarkExpired();

            _reservations.Add(reservation);
        }
    }

    public IEnumerable<Reservation> GetAll() => _reservations;

    public Reservation? GetById(int reservationId)
        => _reservations.FirstOrDefault(r => r.ReservationId == reservationId);

    public void Add(Reservation reservation) => _reservations.Add(reservation);
}