using TrafficCrowdSim.Core.Models;
using TrafficCrowdSim.Core.Network;

namespace TrafficCrowdSim.Core.Control;

public interface ICongestionPolicy
{
    string Name { get; }

    IReadOnlyList<int> PlanRoute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy);

    bool ShouldReroute(
        RoadNetwork network,
        Agent agent,
        IReadOnlyDictionary<int, int> edgeOccupancy,
        int currentTimestep);

    bool ShouldAdmitNewAgent(
        RoadNetwork network,
        int pendingCount,
        IReadOnlyDictionary<int, int> edgeOccupancy);

    /// <summary>Cap injections per timestep so control can pace demand under load.</summary>
    int MaxAdmissionsPerTimestep(
        RoadNetwork network,
        IReadOnlyDictionary<int, int> edgeOccupancy) => int.MaxValue;
}
