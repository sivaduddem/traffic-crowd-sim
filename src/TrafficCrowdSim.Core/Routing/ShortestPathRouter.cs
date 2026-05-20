using TrafficCrowdSim.Core.Network;

namespace TrafficCrowdSim.Core.Routing;

public sealed class ShortestPathRouter
{
    public IReadOnlyList<int> FindRoute(
        RoadNetwork network,
        int originNodeId,
        int destinationNodeId,
        Func<int, double>? edgeWeight = null)
    {
        edgeWeight ??= _ => 1.0;

        var dist = new Dictionary<int, double> { [originNodeId] = 0 };
        var prevEdge = new Dictionary<int, int>();
        var visited = new HashSet<int>();
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(originNodeId, 0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out int node, out double cost);
            if (!visited.Add(node))
                continue;

            if (node == destinationNodeId)
                break;

            foreach (var edge in network.GetOutgoing(node))
            {
                double nextCost = cost + network.Edges[edge.Id].FreeFlowTravelTime * edgeWeight(edge.Id);
                if (!dist.TryGetValue(edge.ToNodeId, out double existing) || nextCost < existing)
                {
                    dist[edge.ToNodeId] = nextCost;
                    prevEdge[edge.ToNodeId] = edge.Id;
                    queue.Enqueue(edge.ToNodeId, nextCost);
                }
            }
        }

        if (!dist.ContainsKey(destinationNodeId))
            return Array.Empty<int>();

        var route = new List<int>();
        int current = destinationNodeId;
        while (current != originNodeId)
        {
            if (!prevEdge.TryGetValue(current, out int edgeId))
                return Array.Empty<int>();
            route.Add(edgeId);
            current = network.Edges[edgeId].FromNodeId;
        }

        route.Reverse();
        return route;
    }
}
