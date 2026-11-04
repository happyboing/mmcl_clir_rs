namespace MMCL_CLIRRS;

public sealed record CountItem(string Label, int Count);

public enum TrendGrouping
{
    Day,
    Week,
    Month,
    Year
}

public class ReservationStatistics
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Occupied { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }

    public int GraphNodes { get; init; }
    public int GraphConnections { get; init; }

    public List<CountItem> StatusCounts { get; init; } = new();
    public List<CountItem> SpotCounts { get; init; } = new();
    public List<CountItem> TrendCounts { get; init; } = new();

    public List<CountItem> DepartmentCounts { get; init; } = new();
    public List<CountItem> ProgramCounts { get; init; } = new();
    public List<CountItem> YearLevelCounts { get; init; } = new();
    public List<CountItem> UserTypeCounts { get; init; } = new();

    public List<GraphNode> TopUsers { get; init; } = new();
    public List<GraphEdge> TopUserLocations { get; init; } = new();
    public List<ActivityLogEntry> RecentActivity { get; init; } = new();
}
