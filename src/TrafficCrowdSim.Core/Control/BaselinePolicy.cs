using TrafficCrowdSim.Core.Models;
using TrafficCrowdSim.Core.Network;
using TrafficCrowdSim.Core.Routing;

namespace TrafficCrowdSim.Core.Control;

/// <summary>Static shortest-path routing with no load-aware admission or rerouting.</summary>
public sealed class BaselinePolicy : ICongestionPolicy
{
    private readonly ShortestPathRouter _router = new();

    public string Name => "Baseline (static shortest path)";

    public IReadOnlyList<int> PlanRoute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy) =>
        _router.FindRoute(network, agent.OriginNodeId, agent.DestinationNodeId);

    public bool ShouldReroute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy,
        int currentTimestep) => false;

    public bool ShouldAdmitNewAgent(
        RoadNetwork network,
        int pendingCount,
        IReadOnlyDictionary<int, int> edgeOccupancy) => true;
}
