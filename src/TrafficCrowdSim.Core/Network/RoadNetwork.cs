using TrafficCrowdSim.Core.Models;

namespace TrafficCrowdSim.Core.Network;

public sealed class RoadNetwork
{
    private readonly Dictionary<int, List<RoadEdge>> _outgoing = new();
    private readonly Dictionary<int, List<RoadEdge>> _incoming = new();

    public IReadOnlyDictionary<int, RoadEdge> Edges { get; }
    public IReadOnlyCollection<int> NodeIds { get; }

    public RoadNetwork(IEnumerable<RoadEdge> edges)
    {
        var edgeList = edges.ToList();
        Edges = edgeList.ToDictionary(e => e.Id);
        NodeIds = edgeList
            .SelectMany(e => new[] { e.FromNodeId, e.ToNodeId })
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        foreach (var nodeId in NodeIds)
        {
            _outgoing[nodeId] = new List<RoadEdge>();
            _incoming[nodeId] = new List<RoadEdge>();
        }

        foreach (var edge in edgeList)
        {
            _outgoing[edge.FromNodeId].Add(edge);
            _incoming[edge.ToNodeId].Add(edge);
        }
    }

    public IEnumerable<RoadEdge> GetOutgoing(int nodeId) => _outgoing[nodeId];
    public IEnumerable<RoadEdge> GetIncoming(int nodeId) => _incoming[nodeId];

    public static RoadNetwork CreateGrid(int rows, int cols, double baseTravelTime = 1.0, int baseCapacity = 4)
    {
        var edges = new List<RoadEdge>();
        int edgeId = 0;

        int Node(int r, int c) => r * cols + c;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int here = Node(r, c);
                if (c + 1 < cols)
                {
                    int right = Node(r, c + 1);
                    edges.Add(new RoadEdge
                    {
                        Id = edgeId++,
                        FromNodeId = here,
                        ToNodeId = right,
                        FreeFlowTravelTime = baseTravelTime,
                        Capacity = baseCapacity
                    });
                    edges.Add(new RoadEdge
                    {
                        Id = edgeId++,
                        FromNodeId = right,
                        ToNodeId = here,
                        FreeFlowTravelTime = baseTravelTime,
                        Capacity = baseCapacity
                    });
                }

                if (r + 1 < rows)
                {
                    int down = Node(r + 1, c);
                    edges.Add(new RoadEdge
                    {
                        Id = edgeId++,
                        FromNodeId = here,
                        ToNodeId = down,
                        FreeFlowTravelTime = baseTravelTime,
                        Capacity = baseCapacity
                    });
                    edges.Add(new RoadEdge
                    {
                        Id = edgeId++,
                        FromNodeId = down,
                        ToNodeId = here,
                        FreeFlowTravelTime = baseTravelTime,
                        Capacity = baseCapacity
                    });
                }
            }
        }

        // Bottleneck corridor to encourage congestion emergence
        int midRow = rows / 2;
        foreach (var edge in edges.Where(e => e.FromNodeId / cols == midRow || e.ToNodeId / cols == midRow).ToList())
        {
            int idx = edges.FindIndex(e => e.Id == edge.Id);
            edges[idx] = new RoadEdge
            {
                Id = edge.Id,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                FreeFlowTravelTime = edge.FreeFlowTravelTime,
                Capacity = Math.Max(2, baseCapacity / 2)
            };
        }

        return new RoadNetwork(edges);
    }
}
