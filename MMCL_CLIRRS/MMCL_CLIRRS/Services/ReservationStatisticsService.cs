namespace MMCL_CLIRRS;

// Builds the staff statistics from the live reservation records.
// The source is the linked list of reservations, the AVL date tree keeps the
// trend data ordered, and the graph ranks user/location relationships.
public class ReservationStatisticsService
{
    private readonly ReservationSearchService _search;

    public ReservationStatisticsService(ReservationSearchService search)
    {
        _search = search;
    }

    public ReservationStatistics Build()
    {
        List<Reservation> all = _search.GetAllChronological();

        var areaBySpot = ClirMapLayout.Spots
            .GroupBy(spot => spot.SpotOrRoom, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Area,
                          StringComparer.OrdinalIgnoreCase);

        ReservationGraph graph = BuildGraph(all);

        return new ReservationStatistics
        {
            Total = all.Count,
            Active = CountStatus(all, ReservationStatus.Active),
            Occupied = CountStatus(all, ReservationStatus.Occupied),
            Completed = CountStatus(all, ReservationStatus.Completed),
            Cancelled = CountStatus(all, ReservationStatus.Cancelled),
            Expired = CountStatus(all, ReservationStatus.Expired),
            DiscussionRoomReservations = all.Count(r => r.IsDiscussionRoom),

            GraphNodes = graph.NodeCount,
            GraphConnections = graph.ConnectionCount,

            StatusCounts = Enum.GetValues<ReservationStatus>()
                .Select(status => new CountItem(status.ToString(), CountStatus(all, status)))
                .ToList(),

            AreaCounts = GroupCounts(all, r => AreaOf(r, areaBySpot)),
            SpotCounts = GroupCounts(all, r => r.SpotOrRoom).Take(12).ToList(),
            DiscussionRoomCounts = GroupCounts(
                all.Where(r => r.IsDiscussionRoom), r => r.SpotOrRoom),
            TrendCounts = BuildTrends(all),

            DepartmentCounts = GroupCounts(all, r => NonEmpty(r.Department, "Unspecified")),
            ProgramCounts = GroupCounts(all, r => NonEmpty(r.Program, "Unspecified")),
            YearLevelCounts = GroupCounts(
                all.Where(r => r.UserType == UserType.Student && r.YearLevel > 0),
                r => $"Year {r.YearLevel}"),
            UserTypeCounts = GroupCounts(all, r => r.UserType.ToString()),

            TopUsers = graph.TopNodes("U:", 8).ToList(),
            TopUserLocations = graph.TopEdges("U:", "S:", 8).ToList(),
            RecentActivity = _search.GetRecentActivity(20)
        };
    }

    private static ReservationGraph BuildGraph(IEnumerable<Reservation> reservations)
    {
        var graph = new ReservationGraph();

        foreach (Reservation reservation in reservations)
        {
            string userNode = $"U:{reservation.UserIdNumber}";
            string spotNode = $"S:{reservation.SpotOrRoom}";
            string reservationNode = $"R:{reservation.ReservationId}";

            graph.AddNode(userNode, $"{reservation.UserName} ({reservation.UserIdNumber})");
            graph.AddNode(spotNode, reservation.SpotOrRoom);
            graph.AddNode(reservationNode, $"Reservation #{reservation.ReservationId}");

            graph.AddEdge(userNode, spotNode);
            graph.AddEdge(userNode, reservationNode);
            graph.AddEdge(reservationNode, spotNode);
        }

        return graph;
    }

    private static List<CountItem> BuildTrends(List<Reservation> all)
    {
        if (all.Count == 0) return new List<CountItem>();

        // Use the AVL tree to keep the days in sorted order before grouping.
        var dateTree = new AvlTree<DateTime>(r => r.Date.Date);
        foreach (Reservation reservation in all)
            dateTree.Insert(reservation);

        DateTime first = all.Min(r => r.Date.Date);
        DateTime last = all.Max(r => r.Date.Date);

        List<Reservation> ordered = dateTree.FindRange(first, last);

        return ordered
            .GroupBy(r => r.Date.Date)
            .Select(group => new CountItem(group.Key.ToString("dd MMM"), group.Count()))
            .ToList();
    }

    private static int CountStatus(IEnumerable<Reservation> reservations,
                                   ReservationStatus status)
        => reservations.Count(r => r.Status == status);

    private static string AreaOf(Reservation reservation,
                                 IReadOnlyDictionary<string, string> areaBySpot)
    {
        if (areaBySpot.TryGetValue(reservation.SpotOrRoom, out string? area))
            return area;

        return reservation.IsDiscussionRoom ? "Discussion Room Wing" : "Other / Unmapped";
    }

    private static List<CountItem> GroupCounts(
        IEnumerable<Reservation> reservations,
        Func<Reservation, string> keySelector)
        => reservations
            .GroupBy(keySelector)
            .Select(group => new CountItem(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();

    private static string NonEmpty(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
