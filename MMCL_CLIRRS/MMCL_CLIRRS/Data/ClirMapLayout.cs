namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #3: CLIR Interactive Seat Map
//  The floor-plan positions of every clickable seat and room.
//  The names come from ClirLayout, so reservations made in
//  Functionality #1 appear on the correct place here.
// ============================================================
public static class ClirMapLayout
{
    public const int MapWidth = 900;
    public const int MapHeight = 558;

    public const int SeatControlWidth = 44;
    public const int SeatControlHeight = 46;
    public const int MonitorSeatWidth = 36;
    public const int MonitorSeatHeight = 44;
    public const int RoomWidth = 100;
    public const int RoomHeight = 190;

    public static Rectangle PieBounds { get; } = new Rectangle(170, 272, 76, 98);

    // Centre of each round table on the floor plan. The positions follow
    // the CLIR sketch: two tables on the left, a large centre table, a
    // large table on the right, and one near the top-right corner.
    public static IReadOnlyList<Point> TableCenters { get; } = new[]
    {
        new Point(100, 285),   // Table 1 - upper-left floor area
        new Point(100, 430),   // Table 2 - lower-left floor area
        new Point(335, 370),   // Table 3 - large centre table
        new Point(650, 330),   // Table 4 - large right-side table
        new Point(680, 155)    // Table 5 - top-right table
    };

    public static IReadOnlyList<ClirMapSpot> Spots { get; } = BuildSpots();

    public static IReadOnlyList<ClirMapSpot> PieSeats { get; } =
        Spots.Where(s => s.Kind == ClirSpotKind.PieSeat)
             .OrderBy(s => s.SeatNumber)
             .ToList();

    private static List<ClirMapSpot> BuildSpots()
    {
        string[] areas =
        {
            "Floor Area",
            "Floor Area",
            "Main Reading Area",
            "Main Reading Area",
            "Window Area"
        };

        var spots = new List<ClirMapSpot>();

        // Four chairs around each round table.
        for (int tableIndex = 0; tableIndex < TableCenters.Count; tableIndex++)
        {
            Point center = TableCenters[tableIndex];
            string tableName = $"Table {tableIndex + 1}";

            for (int seat = 1; seat <= ClirLayout.SeatsPerTable; seat++)
            {
                Point chairCenter = seat switch
                {
                    1 => new Point(center.X - 46, center.Y - 46),
                    2 => new Point(center.X + 46, center.Y - 46),
                    3 => new Point(center.X + 46, center.Y + 46),
                    _ => new Point(center.X - 46, center.Y + 46)
                };

                var bounds = new Rectangle(
                    chairCenter.X - (SeatControlWidth / 2),
                    chairCenter.Y - 12,
                    SeatControlWidth,
                    SeatControlHeight);

                spots.Add(new ClirMapSpot(
                    $"{tableName} - Seat {seat}",
                    ClirSpotKind.Seat,
                    tableName,
                    areas[tableIndex],
                    seat,
                    bounds));
            }
        }

        // One seat under each of the five monitors.
        for (int i = 0; i < ClirLayout.MonitorSeatCount; i++)
        {
            var bounds = new Rectangle(
                38 + i * 36 - (MonitorSeatWidth / 2),
                130 - 12,
                MonitorSeatWidth,
                MonitorSeatHeight);

            spots.Add(new ClirMapSpot(
                $"Monitor Seat {i + 1}",
                ClirSpotKind.MonitorSeat,
                "—",
                "Monitor Area",
                i + 1,
                bounds));
        }

        // Each of the eight pie cuts is a seat.
        for (int i = 0; i < ClirLayout.PieSeatCount; i++)
        {
            spots.Add(new ClirMapSpot(
                $"Pie Seat {i + 1}",
                ClirSpotKind.PieSeat,
                "—",
                "Floor Area",
                i + 1,
                PieBounds));
        }

        // Five larger bean bags along the top wall.
        for (int i = 0; i < 5; i++)
        {
            spots.Add(new ClirMapSpot(
                $"Bean Bag {i + 1}",
                ClirSpotKind.BeanBag,
                "—",
                "Window Area",
                i + 1,
                new Rectangle(210 + i * 62, 8, 56, 58)));
        }

        // Another five bean bags above the row of small tables.
        for (int i = 0; i < ClirLayout.BeanBagTableCount; i++)
        {
            int cellX = 210 + i * 62;
            spots.Add(new ClirMapSpot(
                $"Bean Bag {i + 6}",
                ClirSpotKind.BeanBag,
                "—",
                "Main Reading Area",
                i + 6,
                new Rectangle(cellX + 1, 80, 56, 58)));
        }

        // Five small tables, each with one solo seat below it.
        for (int i = 0; i < ClirLayout.BeanBagTableCount; i++)
        {
            int cellX = 210 + i * 62;
            spots.Add(new ClirMapSpot(
                $"Bean Bag Table Seat {i + 1}",
                ClirSpotKind.Seat,
                $"Bean Bag Table {i + 1}",
                "Main Reading Area",
                1,
                new Rectangle(cellX + 7, 209, SeatControlWidth, SeatControlHeight)));
        }

        // Couches in the middle and along the bottom.
        spots.Add(new ClirMapSpot("Couch 1", ClirSpotKind.Couch, "—", "Lounge Area", 1,
            new Rectangle(464, 350, 46, 140)));
        spots.Add(new ClirMapSpot("Couch 2", ClirSpotKind.Couch, "—", "Lounge Area", 2,
            new Rectangle(514, 350, 46, 140)));
        spots.Add(new ClirMapSpot("Couch 3", ClirSpotKind.Couch, "—", "Lounge Area", 3,
            new Rectangle(500, 490, 220, 60)));

        // Three loose floor chairs at the bottom.
        for (int i = 0; i < ClirLayout.FloorChairCount; i++)
        {
            spots.Add(new ClirMapSpot(
                $"Floor Chair {i + 1}",
                ClirSpotKind.Seat,
                "—",
                "Floor Area",
                i + 1,
                new Rectangle(230 + i * 65, 498, 55, 44)));
        }

        // The two open floor areas are reservable too.
        spots.Add(new ClirMapSpot("Floor Area 1", ClirSpotKind.FloorArea, "—",
            "Floor Area", 0, new Rectangle(16, 181, 150, 44)));
        spots.Add(new ClirMapSpot("Floor Area 2", ClirSpotKind.FloorArea, "—",
            "Floor Area", 0, new Rectangle(176, 452, 150, 44)));

        // The small rectangular reading table.
        spots.Add(new ClirMapSpot("Small Table", ClirSpotKind.Table, "—",
            "Main Reading Area", 0, new Rectangle(585, 420, 100, 50)));

        // The sketch has one discussion room.
        for (int room = 1; room <= ClirLayout.DiscussionRoomCount; room++)
        {
            spots.Add(new ClirMapSpot(
                $"Discussion Room {room}",
                ClirSpotKind.DiscussionRoom,
                "—",
                "Discussion Room Wing",
                0,
                new Rectangle(776, 195, RoomWidth, RoomHeight)));
        }

        return spots;
    }
}
