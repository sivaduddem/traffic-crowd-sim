namespace TrafficCrowdSim.Core.Simulation;

public sealed class SimulationConfig
{
    public int Timesteps { get; init; } = 200;
    public int AgentCount { get; init; } = 80;
    public int Seed { get; init; } = 42;
    public int GridRows { get; init; } = 6;
    public int GridCols { get; init; } = 6;
    public double BaseTravelTime { get; init; } = 1.0;
    public int BaseEdgeCapacity { get; init; } = 4;
}
