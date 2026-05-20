using TrafficCrowdSim.Core.Congestion;
using TrafficCrowdSim.Core.Models;
using TrafficCrowdSim.Core.Network;
using TrafficCrowdSim.Core.Routing;

namespace TrafficCrowdSim.Core.Control;

/// <summary>
/// Load-aware routing, paced admission, and rerouting to maximize route completions under congestion.
/// </summary>
public sealed class AdaptiveCongestionPolicy : ICongestionPolicy
{
    private readonly ShortestPathRouter _router = new();

    public double LoadWeightFactor { get; init; } = 5.0;
    public double RerouteDelayThreshold { get; init; } = 1.35;
    public double MaxNetworkOccupancyForAdmission { get; init; } = 0.82;
    public double RerouteOccupancyThreshold { get; init; } = 0.70;

    public string Name => "Adaptive (load-aware + admission control)";

    public IReadOnlyList<int> PlanRoute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy)
    {
        return _router.FindRoute(
            network,
            agent.OriginNodeId,
            agent.DestinationNodeId,
            edgeId =>
            {
                var edge = network.Edges[edgeId];
                edgeOccupancy.TryGetValue(edgeId, out int load);
                return CongestionModel.EffectiveTravelTime(edge, load) * (1.0 + LoadWeightFactor * 0.1);
            });
    }

    public bool ShouldReroute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy,
        int currentTimestep)
    {
        if (agent.CurrentEdgeId is null || agent.RouteEdgeIds.Count == 0)
            return false;

        int edgeId = agent.CurrentEdgeId.Value;
        if (!edgeOccupancy.TryGetValue(edgeId, out int load))
            return false;

        var edge = network.Edges[edgeId];
        double occ = CongestionModel.OccupancyRatio(load, edge.Capacity);

        double waited = currentTimestep - agent.DepartureTimestep;
        double expected = agent.RouteEdgeIds
            .Take(agent.RouteIndex + 1)
            .Sum(id => network.Edges[id].FreeFlowTravelTime);

        return waited > expected * RerouteDelayThreshold && occ >= RerouteOccupancyThreshold;
    }

    public bool ShouldAdmitNewAgent(
        RoadNetwork network,
        int pendingCount,
        IReadOnlyDictionary<int, int> edgeOccupancy) =>
        NetworkPressure(network, edgeOccupancy) < MaxNetworkOccupancyForAdmission;

    public int MaxAdmissionsPerTimestep(
        RoadNetwork network,
        IReadOnlyDictionary<int, int> edgeOccupancy)
    {
        double pressure = NetworkPressure(network, edgeOccupancy);
        if (pressure < 0.35)
            return 8;
        if (pressure < 0.55)
            return 4;
        if (pressure < MaxNetworkOccupancyForAdmission)
            return 2;
        return 0;
    }

    private static double NetworkPressure(
        RoadNetwork network,
        IReadOnlyDictionary<int, int> edgeOccupancy)
    {
        if (edgeOccupancy.Count == 0)
            return 0;

        var ratios = edgeOccupancy.Select(kv =>
            CongestionModel.OccupancyRatio(kv.Value, network.Edges[kv.Key].Capacity)).ToList();

        return 0.55 * ratios.Average() + 0.45 * ratios.Max();
    }
}
