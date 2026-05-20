using TrafficCrowdSim.Core.Models;

namespace TrafficCrowdSim.Core.Congestion;

/// <summary>
/// BPR-style delay: travel time grows superlinearly as edge load approaches capacity,
/// producing emergent congestion and tail-latency spikes under high agent density.
/// </summary>
public static class CongestionModel
{
    public static double OccupancyRatio(int agentsOnEdge, int capacity) =>
        capacity <= 0 ? 1.0 : agentsOnEdge / (double)capacity;

    public static double TravelTimeMultiplier(double occupancyRatio, double alpha = 0.15, double beta = 4.0) =>
        1.0 + alpha * Math.Pow(Math.Min(occupancyRatio, 2.5), beta);

    public static double EffectiveTravelTime(RoadEdge edge, int agentsOnEdge) =>
        edge.FreeFlowTravelTime * TravelTimeMultiplier(OccupancyRatio(agentsOnEdge, edge.Capacity));
}
