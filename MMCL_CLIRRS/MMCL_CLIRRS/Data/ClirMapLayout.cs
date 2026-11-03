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

    public const int SeatControlWidth = 72;
    public const int SeatControlHeight = 52;
    public const int RoomWidth = 100;
    public const int RoomHeight = 92;

    // Centre of each round table on the floor plan.
    public static IReadOnlyList<Point> TableCenters { get; } = new[]
    {
        new Point(150, 140),   // Table 1 - window area
        new Point(400, 140),   // Table 2 - main reading area
        new Point(650, 140),   // Table 3 - main reading area
        new Point(260, 390),   // Table 4 - floor area
        new Point(530, 390)    // Table 5 - main reading area
    };

    public static IReadOnlyList<ClirMapSpot> Spots { get; } = BuildSpots();

    private static List<ClirMapSpot> BuildSpots()
    {
        string[] areas =
        {
            "Window Area",
            "Main Reading Area",
            "Main Reading Area",
            "Floor Area",
            "Main Reading Area"
        };

        var spots = new List<ClirMapSpot>();

        for (int tableIndex = 0; tableIndex < TableCenters.Count; tableIndex++)
        {
            Point center = TableCenters[tableIndex];
            string tableName = $"Table {tableIndex + 1}";

            for (int seat = 1; seat <= ClirLayout.SeatsPerTable; seat++)
            {
                // Seats sit at the top, right, bottom and left of the table.
                // The control is a little taller than the chair so a "Cancel"
                // button can appear directly below a reserved seat.
                Point chairCenter = seat switch
                {
                    1 => new Point(center.X, center.Y - 88),
                    2 => new Point(center.X + 88, center.Y),
                    3 => new Point(center.X, center.Y + 88),
                    _ => new Point(center.X - 88, center.Y)
                };

                var bounds = new Rectangle(
                    chairCenter.X - (SeatControlWidth / 2),
                    chairCenter.Y - 14,
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

        for (int room = 1; room <= ClirLayout.DiscussionRoomCount; room++)
        {
            var bounds = new Rectangle(790, 50 + (room - 1) * 102, RoomWidth, RoomHeight);
            spots.Add(new ClirMapSpot(
                $"Discussion Room {room}",
                ClirSpotKind.DiscussionRoom,
                "—",
                "Discussion Room Wing",
                0,
                bounds));
        }

        return spots;
    }
}
