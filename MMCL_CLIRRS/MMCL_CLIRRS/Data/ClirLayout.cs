namespace MMCL_CLIRRS;

public static class ClirLayout
{
    public const int TableCount = 5;
    public const int SeatsPerTable = 4;
    public const int DiscussionRoomCount = 1;

    public const int MonitorSeatCount = 5;
    public const int PieSeatCount = 8;
    public const int CouchCount = 3;
    public const int BeanBagCount = 10;
    public const int BeanBagTableCount = 5;
    public const int FloorChairCount = 3;
    public const int FloorAreaCount = 2;

    public static List<string> GetSeats()
    {
        var seats = new List<string>();

        for (int table = 1; table <= TableCount; table++)
        {
            for (int seat = 1; seat <= SeatsPerTable; seat++)
                seats.Add($"Table {table} - Seat {seat}");
        }

        for (int i = 1; i <= MonitorSeatCount; i++)
            seats.Add($"Monitor Seat {i}");

        for (int i = 1; i <= PieSeatCount; i++)
            seats.Add($"Pie Seat {i}");

        for (int i = 1; i <= BeanBagCount; i++)
            seats.Add($"Bean Bag {i}");

        for (int i = 1; i <= BeanBagTableCount; i++)
            seats.Add($"Bean Bag Table Seat {i}");

        for (int i = 1; i <= CouchCount; i++)
            seats.Add($"Couch {i}");

        for (int i = 1; i <= FloorChairCount; i++)
            seats.Add($"Floor Chair {i}");

        for (int i = 1; i <= FloorAreaCount; i++)
            seats.Add($"Floor Area {i}");

        seats.Add("Small Table");
        return seats;
    }

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
