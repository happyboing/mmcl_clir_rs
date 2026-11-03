namespace MMCL_CLIRRS;

// The list of every seat and room inside the CLIR.
// Want more tables or rooms? Change the three numbers below.
// (Functionality #3's seat map can read from here too.)
public static class ClirLayout
{
    public const int TableCount = 5;
    public const int SeatsPerTable = 4;
    public const int DiscussionRoomCount = 3;

    // "Table 1 - Seat 1", "Table 1 - Seat 2", ... (same naming as the sample data)
    public static List<string> GetSeats()
    {
        var seats = new List<string>();
        for (int table = 1; table <= TableCount; table++)
        {
            for (int seat = 1; seat <= SeatsPerTable; seat++)
                seats.Add($"Table {table} - Seat {seat}");
        }
        return seats;
    }

    // "Discussion Room 1", "Discussion Room 2", ...
    public static List<string> GetDiscussionRooms()
    {
        var rooms = new List<string>();
        for (int room = 1; room <= DiscussionRoomCount; room++)
            rooms.Add($"Discussion Room {room}");
        return rooms;
    }

    public static bool Exists(string spotOrRoom)
        => GetSeats().Contains(spotOrRoom) || GetDiscussionRooms().Contains(spotOrRoom);
}
