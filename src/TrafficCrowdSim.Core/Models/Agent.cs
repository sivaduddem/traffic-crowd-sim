namespace TrafficCrowdSim.Core.Models;

/// <summary>Distributed traveler traversing the shared road network.</summary>
public sealed class Agent
{
    public required int Id { get; init; }
    public required int OriginNodeId { get; init; }
    public required int DestinationNodeId { get; init; }
    public int CurrentNodeId { get; set; }
    public int? CurrentEdgeId { get; set; }
    public double ProgressOnEdge { get; set; }
    public IReadOnlyList<int> RouteEdgeIds { get; set; } = Array.Empty<int>();
    public int RouteIndex { get; set; }
    public int DepartureTimestep { get; set; }
    public int? ArrivalTimestep { get; set; }
    public bool IsActive => ArrivalTimestep is null;
}
