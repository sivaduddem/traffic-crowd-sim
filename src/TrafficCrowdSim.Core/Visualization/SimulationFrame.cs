namespace TrafficCrowdSim.Core.Visualization;

public sealed class SimulationRecording
{
    public required int GridRows { get; init; }
    public required int GridCols { get; init; }
    public required string PolicyName { get; init; }
    public required IReadOnlyList<SimulationFrame> Frames { get; init; }
}

public sealed class SimulationFrame
{
    public required int Timestep { get; init; }
    public required int CompletionsThisStep { get; init; }
    public required int ActiveAgents { get; init; }
    public required int PendingAgents { get; init; }
    public required int TotalCompletions { get; init; }
    public required IReadOnlyList<EdgeView> Edges { get; init; }
    public required IReadOnlyList<AgentView> Agents { get; init; }
}

public sealed class EdgeView
{
    public required int Id { get; init; }
    public required int FromNodeId { get; init; }
    public required int ToNodeId { get; init; }
    public required int Load { get; init; }
    public required int Capacity { get; init; }
    public required double OccupancyRatio { get; init; }
}

public sealed class AgentView
{
    public required int Id { get; init; }
    public required int OriginNodeId { get; init; }
    public required int DestinationNodeId { get; init; }
    public required int CurrentNodeId { get; init; }
    public int? CurrentEdgeId { get; init; }
    public required double ProgressOnEdge { get; init; }
    public required bool IsActive { get; init; }
}
