namespace MMCL_CLIRRS;

public enum ClirSpotKind
{
    Seat,
    DiscussionRoom
}

// One selectable place on the interactive CLIR map, together with the
// information shown in the details panel when it is selected.
public sealed record ClirMapSpot(
    string SpotOrRoom,
    ClirSpotKind Kind,
    string Table,
    string Area,
    int SeatNumber,
    Rectangle Bounds)
{
    public bool IsDiscussionRoom => Kind == ClirSpotKind.DiscussionRoom;
}
