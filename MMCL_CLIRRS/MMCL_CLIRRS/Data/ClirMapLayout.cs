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
    public const int RoomWidth = 96;
    public const int RoomHeight = 62;

    // Centre of each round table on the floor plan. The positions follow
    // the CLIR sketch: two tables on the left, a large centre table, a
    // large table on the right, and one near the top-right corner.
    public static IReadOnlyList<Point> TableCenters { get; } = new[]
    {
        new Point(100, 285),   // Table 1 - upper-left floor area
        new Point(100, 430),   // Table 2 - lower-left floor area
        new Point(335, 370),   // Table 3 - large centre table
        new Point(650, 330),   // Table 4 - large right-side table
        new Point(600, 155)    // Table 5 - top-right table
    };

    public static IReadOnlyList<ClirMapSpot> Spots { get; } = BuildSpots();

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

        for (int tableIndex = 0; tableIndex < TableCenters.Count; tableIndex++)
        {
            Point center = TableCenters[tableIndex];
            string tableName = $"Table {tableIndex + 1}";

            for (int seat = 1; seat <= ClirLayout.SeatsPerTable; seat++)
            {
                // The sketch shows chairs around the four corners of each
                // table, so the seats sit diagonally instead of in a cross.
                // The control includes a thin "Cancel" strip below the chair.
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

        for (int room = 1; room <= ClirLayout.DiscussionRoomCount; room++)
        {
            var bounds = new Rectangle(780, 300 + (room - 1) * 70, RoomWidth, RoomHeight);
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
