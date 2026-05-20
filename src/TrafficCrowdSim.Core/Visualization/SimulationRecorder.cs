using TrafficCrowdSim.Core.Congestion;
using TrafficCrowdSim.Core.Control;
using TrafficCrowdSim.Core.Models;
using TrafficCrowdSim.Core.Network;
using TrafficCrowdSim.Core.Simulation;

namespace TrafficCrowdSim.Core.Visualization;

public static class SimulationRecorder
{
    public static SimulationRecording Record(
        RoadNetwork network,
        ICongestionPolicy policy,
        SimulationConfig config,
        int snapshotEvery = 1)
    {
        var simulator = new TrafficSimulator(network, policy, config);
        var frames = new List<SimulationFrame>();
        int totalCompletions = 0;

        while (simulator.Step(out int completions))
        {
            totalCompletions += completions;
            if (simulator.Timestep % snapshotEvery != 0)
                continue;

            frames.Add(CaptureFrame(
                simulator, network, simulator.Timestep - 1, completions, totalCompletions));
        }

        return new SimulationRecording
        {
            GridRows = config.GridRows,
            GridCols = config.GridCols,
            PolicyName = policy.Name,
            Frames = frames
        };
    }

    private static SimulationFrame CaptureFrame(
        TrafficSimulator simulator,
        RoadNetwork network,
        int timestep,
        int completions,
        int totalCompletions)
    {
        var occupancy = simulator.GetEdgeOccupancy();
        var edges = network.Edges.Values.Select(edge =>
        {
            occupancy.TryGetValue(edge.Id, out int load);
            return new EdgeView
            {
                Id = edge.Id,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                Load = load,
                Capacity = edge.Capacity,
                OccupancyRatio = CongestionModel.OccupancyRatio(load, edge.Capacity)
            };
        }).ToList();

        var agents = simulator.Agents.Select(a => new AgentView
        {
            Id = a.Id,
            OriginNodeId = a.OriginNodeId,
            DestinationNodeId = a.DestinationNodeId,
            CurrentNodeId = a.CurrentNodeId,
            CurrentEdgeId = a.CurrentEdgeId,
            ProgressOnEdge = a.ProgressOnEdge,
            IsActive = a.IsActive
        }).ToList();

        return new SimulationFrame
        {
            Timestep = timestep,
            CompletionsThisStep = completions,
            ActiveAgents = agents.Count(a => a.IsActive),
            PendingAgents = simulator.PendingCount,
            TotalCompletions = totalCompletions,
            Edges = edges,
            Agents = agents
        };
    }
}
