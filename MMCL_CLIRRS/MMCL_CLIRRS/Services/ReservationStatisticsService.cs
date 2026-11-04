namespace MMCL_CLIRRS;

public class ReservationStatisticsService
{
    private readonly ReservationSearchService _search;

    public ReservationStatisticsService(ReservationSearchService search)
    {
        _search = search;
    }

    public ReservationStatistics Build(TrendGrouping trendGrouping = TrendGrouping.Day)
    {
        List<Reservation> all = _search.GetAllChronological()
            .Where(r => r.Status != ReservationStatus.Expired)
            .ToList();

        ReservationGraph graph = BuildGraph(all);

        return new ReservationStatistics
        {
            Total = all.Count,
            Active = CountStatus(all, ReservationStatus.Active),
            Occupied = CountStatus(all, ReservationStatus.Occupied),
            Completed = CountStatus(all, ReservationStatus.Completed),
            Cancelled = CountStatus(all, ReservationStatus.Cancelled),

            GraphNodes = graph.NodeCount,
            GraphConnections = graph.ConnectionCount,

            StatusCounts = new List<CountItem>
            {
                new("Active", CountStatus(all, ReservationStatus.Active)),
                new("Occupied", CountStatus(all, ReservationStatus.Occupied)),
                new("Completed", CountStatus(all, ReservationStatus.Completed)),
                new("Cancelled", CountStatus(all, ReservationStatus.Cancelled))
            },

            SpotCounts = GroupCounts(all, r => r.SpotOrRoom).Take(12).ToList(),
            TrendCounts = BuildTrends(all, trendGrouping),

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

    private static List<CountItem> BuildTrends(List<Reservation> all, TrendGrouping grouping)
    {
        if (all.Count == 0) return new List<CountItem>();

        var dateTree = new AvlTree<DateTime>(r => r.Date.Date);
        foreach (Reservation reservation in all)
            dateTree.Insert(reservation);

        DateTime first = all.Min(r => r.Date.Date);
        DateTime last = all.Max(r => r.Date.Date);

        List<Reservation> ordered = dateTree.FindRange(first, last);

        return ordered
            .GroupBy(r => PeriodKey(r.Date.Date, grouping))
            .Select(group => new CountItem(PeriodLabel(group.Key, grouping), group.Count()))
            .ToList();
    }

    private static DateTime PeriodKey(DateTime date, TrendGrouping grouping)
        => grouping switch
        {
            TrendGrouping.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            TrendGrouping.Month => new DateTime(date.Year, date.Month, 1),
            TrendGrouping.Year => new DateTime(date.Year, 1, 1),
            _ => date.Date
        };

    private static string PeriodLabel(DateTime date, TrendGrouping grouping)
        => grouping switch
        {
            TrendGrouping.Week => $"Week of {date:dd MMM}",
            TrendGrouping.Month => date.ToString("MMM yyyy"),
            TrendGrouping.Year => date.ToString("yyyy"),
            _ => date.ToString("dd MMM")
        };

    private static int CountStatus(IEnumerable<Reservation> reservations,
                                   ReservationStatus status)
        => reservations.Count(r => r.Status == status);

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
