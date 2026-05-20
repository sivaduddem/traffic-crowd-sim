namespace TrafficCrowdSim.Core.Models;

/// <summary>Directed road segment between two intersections.</summary>
public sealed class RoadEdge
{
    public required int Id { get; init; }
    public required int FromNodeId { get; init; }
    public required int ToNodeId { get; init; }
    /// <summary>Free-flow travel time in simulation timesteps.</summary>
    public required double FreeFlowTravelTime { get; init; }
    /// <summary>Maximum agents that can occupy the edge without severe slowdown.</summary>
    public required int Capacity { get; init; }
}
