namespace MMCL_CLIRRS;

// Remembers the seat map's last view while the app is open, so going
// Back to Home and reopening the map does not reset the date/time or spot.
public static class SeatMapSession
{
    public static DateTime? ViewDate { get; set; }
    public static TimeSpan? ViewTime { get; set; }
    public static string? SelectedSpot { get; set; }
}
