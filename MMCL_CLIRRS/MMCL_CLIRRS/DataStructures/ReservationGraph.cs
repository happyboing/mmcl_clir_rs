namespace MMCL_CLIRRS;

public sealed record GraphNode(string Id, string Label, int Weight);
public sealed record GraphEdge(string FromId, string FromLabel,
                               string ToId, string ToLabel, int Weight);

// Weighted graph used by the statistics screen:
//   user ── reserves ──> location
//   user ── owns ──────> reservation
//   reservation ───────> location
// This makes user/location/reservation relationships easy to rank.
public class ReservationGraph
{
    private readonly Dictionary<string, string> _labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, int>> _edges =
        new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _labels.Count;

    public int ConnectionCount
        => _edges.Values.Sum(neighbors => neighbors.Count);

    public void AddNode(string id, string label)
    {
        if (!_labels.ContainsKey(id)) _labels[id] = label;
        if (!_edges.ContainsKey(id))
            _edges[id] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public void AddEdge(string fromId, string toId, int weight = 1)
    {
        AddNode(fromId, fromId);
        AddNode(toId, toId);

        Dictionary<string, int> neighbors = _edges[fromId];
        neighbors[toId] = neighbors.GetValueOrDefault(toId) + weight;
    }

    // Highest total outgoing weight among nodes with the given prefix.
    public IReadOnlyList<GraphNode> TopNodes(string prefix, int count)
    {
        return _edges
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(pair => new
            {
                Id = pair.Key,
                Weight = pair.Value.Values.Sum()
            })
            .Where(item => item.Weight > 0)
            .OrderByDescending(item => item.Weight)
            .ThenBy(item => _labels[item.Id])
            .Take(count)
            .Select(item => new GraphNode(item.Id, _labels[item.Id], item.Weight))
            .ToList();
    }

    // Strongest connections between a "from" category and a "to" category.
    public IReadOnlyList<GraphEdge> TopEdges(string fromPrefix, string toPrefix, int count)
    {
        var result = new List<GraphEdge>();

        foreach ((string fromId, Dictionary<string, int> neighbors) in _edges)
        {
            if (!fromId.StartsWith(fromPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach ((string toId, int weight) in neighbors)
            {
                if (!toId.StartsWith(toPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(new GraphEdge(
                    fromId, _labels[fromId],
                    toId, _labels[toId],
                    weight));
            }
        }

        return result
            .OrderByDescending(edge => edge.Weight)
            .ThenBy(edge => edge.FromLabel)
            .ThenBy(edge => edge.ToLabel)
            .Take(count)
            .ToList();
    }
}
