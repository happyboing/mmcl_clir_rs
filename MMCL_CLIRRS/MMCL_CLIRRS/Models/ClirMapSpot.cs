namespace MMCL_CLIRRS;

public enum ClirSpotKind
{
    Seat,
    MonitorSeat,
    PieSeat,
    BeanBag,
    Couch,
    Table,
    FloorArea,
    DiscussionRoom
}

public sealed record ClirMapSpot(
    string SpotOrRoom,
    ClirSpotKind Kind,
    string Table,
    string Area,
    int SeatNumber,
    Rectangle Bounds)
{
    public bool IsDiscussionRoom => Kind == ClirSpotKind.DiscussionRoom;

    public string TypeLabel => Kind switch
    {
        ClirSpotKind.DiscussionRoom => "Discussion Room",
        ClirSpotKind.MonitorSeat => "Monitor Seat",
        ClirSpotKind.PieSeat => "Pie Seat",
        ClirSpotKind.BeanBag => "Bean Bag",
        ClirSpotKind.Couch => "Couch",
        ClirSpotKind.Table => "Table",
        ClirSpotKind.FloorArea => "Floor Area",
        _ => "Seat"
    };
}
