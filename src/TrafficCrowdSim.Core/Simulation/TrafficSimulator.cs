using TrafficCrowdSim.Core.Congestion;
using TrafficCrowdSim.Core.Control;
using TrafficCrowdSim.Core.Metrics;
using TrafficCrowdSim.Core.Models;
using TrafficCrowdSim.Core.Network;

namespace TrafficCrowdSim.Core.Simulation;

public sealed class TrafficSimulator
{
    private readonly RoadNetwork _network;
    private readonly ICongestionPolicy _policy;
    private readonly SimulationConfig _config;
    private readonly Random _rng;

    private readonly List<Agent> _agents = new();
    private readonly Queue<AgentRequest> _pending = new();
    private readonly Dictionary<int, int> _edgeOccupancy = new();
    private readonly SimulationMetrics _metrics = new();

    private record AgentRequest(int Origin, int Destination);

    public TrafficSimulator(RoadNetwork network, ICongestionPolicy policy, SimulationConfig config)
    {
        _network = network;
        _policy = policy;
        _config = config;
        _rng = new Random(config.Seed);
        EnqueueDemand();
    }

    public RoadNetwork Network => _network;
    public ICongestionPolicy Policy => _policy;
    public IReadOnlyList<Agent> Agents => _agents;
    public int PendingCount => _pending.Count;
    public int Timestep { get; private set; }

    public SimulationMetrics Run()
    {
        while (Step(out _))
        {
        }

        return _metrics;
    }

    public bool Step(out int completions)
    {
        completions = 0;
        if (Timestep >= _config.Timesteps)
            return false;

        AdmitAgents(Timestep);
        TryRerouteActiveAgents(Timestep);
        completions = AdvanceAgents(Timestep);
        _metrics.RecordTimestep(
            completions,
            _agents.Count(a => a.IsActive),
            MeanOccupancy());

        Timestep++;
        return Timestep < _config.Timesteps;
    }

    public IReadOnlyDictionary<int, int> GetEdgeOccupancy()
    {
        RebuildOccupancy();
        return new Dictionary<int, int>(_edgeOccupancy);
    }

    private void EnqueueDemand()
    {
        var nodes = _network.NodeIds.ToList();
        for (int i = 0; i < _config.AgentCount; i++)
        {
            int origin = nodes[_rng.Next(nodes.Count)];
            int dest = nodes[_rng.Next(nodes.Count)];
            while (dest == origin)
                dest = nodes[_rng.Next(nodes.Count)];

            _pending.Enqueue(new AgentRequest(origin, dest));
        }
    }

    private void AdmitAgents(int timestep)
    {
        RebuildOccupancy();
        int nextId = _agents.Count;
        int admitted = 0;
        int maxAdmissions = _policy.MaxAdmissionsPerTimestep(_network, _edgeOccupancy);

        while (_pending.Count > 0 && admitted < maxAdmissions)
        {
            if (!_policy.ShouldAdmitNewAgent(_network, _pending.Count, _edgeOccupancy))
                break;

            var req = _pending.Dequeue();
            var agent = new Agent
            {
                Id = nextId++,
                OriginNodeId = req.Origin,
                DestinationNodeId = req.Destination,
                CurrentNodeId = req.Origin,
                DepartureTimestep = timestep
            };

            agent.RouteEdgeIds = _policy.PlanRoute(_network, agent, _edgeOccupancy);
            if (agent.RouteEdgeIds.Count == 0)
            {
                _pending.Enqueue(req);
                break;
            }

            _agents.Add(agent);
            admitted++;
        }
    }

    private void TryRerouteActiveAgents(int timestep)
    {
        foreach (var agent in _agents.Where(a => a.IsActive))
        {
            if (!_policy.ShouldReroute(_network, agent, _edgeOccupancy, timestep))
                continue;

            var fromCurrent = new Agent
            {
                Id = agent.Id,
                OriginNodeId = agent.CurrentNodeId,
                DestinationNodeId = agent.DestinationNodeId,
                CurrentNodeId = agent.CurrentNodeId
            };
            var newRoute = _policy.PlanRoute(_network, fromCurrent, _edgeOccupancy);
            if (newRoute.Count == 0)
                continue;

            agent.RouteEdgeIds = newRoute;
            agent.RouteIndex = 0;
            agent.CurrentEdgeId = null;
            agent.ProgressOnEdge = 0;
        }
    }

    private int AdvanceAgents(int timestep, SimulationMetrics? metrics = null)
    {
        metrics ??= _metrics;
        RebuildOccupancy();
        int completions = 0;

        foreach (var agent in _agents.Where(a => a.IsActive))
        {
            if (agent.CurrentEdgeId is null)
            {
                if (agent.RouteIndex >= agent.RouteEdgeIds.Count)
                {
                    if (agent.CurrentNodeId == agent.DestinationNodeId)
                    {
                        agent.ArrivalTimestep = timestep;
                        metrics.RecordCompletion(timestep - agent.DepartureTimestep);
                        completions++;
                    }
                    continue;
                }

                int nextEdgeId = agent.RouteEdgeIds[agent.RouteIndex];
                var nextEdge = _network.Edges[nextEdgeId];
                if (agent.CurrentNodeId != nextEdge.FromNodeId)
                    continue;

                _edgeOccupancy.TryGetValue(nextEdgeId, out int edgeLoad);
                if (edgeLoad >= nextEdge.Capacity)
                    continue;

                agent.CurrentEdgeId = nextEdgeId;
                agent.ProgressOnEdge = 0;
            }

            int edgeId = agent.CurrentEdgeId!.Value;
            var edge = _network.Edges[edgeId];
            _edgeOccupancy.TryGetValue(edgeId, out int load);
            double travelTime = CongestionModel.EffectiveTravelTime(edge, load);
            double progressRate = 1.0 / travelTime;
            agent.ProgressOnEdge += progressRate;

            if (agent.ProgressOnEdge < 1.0)
                continue;

            agent.ProgressOnEdge = 0;
            agent.CurrentEdgeId = null;
            agent.CurrentNodeId = edge.ToNodeId;
            agent.RouteIndex++;

            if (agent.CurrentNodeId == agent.DestinationNodeId)
            {
                agent.ArrivalTimestep = timestep;
                metrics.RecordCompletion(timestep - agent.DepartureTimestep);
                completions++;
            }
        }

        return completions;
    }

    private void RebuildOccupancy()
    {
        _edgeOccupancy.Clear();
        foreach (var agent in _agents.Where(a => a.IsActive && a.CurrentEdgeId is not null))
        {
            int edgeId = agent.CurrentEdgeId!.Value;
            _edgeOccupancy[edgeId] = _edgeOccupancy.GetValueOrDefault(edgeId) + 1;
        }
    }

    private double MeanOccupancy()
    {
        if (_edgeOccupancy.Count == 0)
            return 0;

        return _edgeOccupancy.Values.Average();
    }
}
