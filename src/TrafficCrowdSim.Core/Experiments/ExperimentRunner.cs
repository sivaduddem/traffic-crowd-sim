using TrafficCrowdSim.Core.Control;
using TrafficCrowdSim.Core.Metrics;
using TrafficCrowdSim.Core.Network;
using TrafficCrowdSim.Core.Simulation;

namespace TrafficCrowdSim.Core.Experiments;

public static class ExperimentRunner
{
    public static (SimulationMetrics Baseline, SimulationMetrics Adaptive) ComparePolicies(
        SimulationConfig config,
        AdaptiveCongestionPolicy? adaptivePolicy = null)
    {
        var network = RoadNetwork.CreateGrid(
            config.GridRows,
            config.GridCols,
            config.BaseTravelTime,
            config.BaseEdgeCapacity);

        var policy = adaptivePolicy ?? new AdaptiveCongestionPolicy();
        var baselineNetwork = RoadNetwork.CreateGrid(
            config.GridRows, config.GridCols, config.BaseTravelTime, config.BaseEdgeCapacity);
        var adaptiveNetwork = RoadNetwork.CreateGrid(
            config.GridRows, config.GridCols, config.BaseTravelTime, config.BaseEdgeCapacity);

        var baseline = new TrafficSimulator(baselineNetwork, new BaselinePolicy(), config).Run();
        var adaptive = new TrafficSimulator(adaptiveNetwork, policy, config).Run();
        return (baseline, adaptive);
    }

    /// <summary>Grid-search policy knobs for highest mean completions per timestep vs baseline.</summary>
    public static AdaptiveCongestionPolicy TuneAdaptivePolicy(SimulationConfig config)
    {
        double[] loadWeights = [3.0, 5.0, 7.0];
        double[] admissionCeilings = [0.75, 0.82, 0.90];
        double[] rerouteThresholds = [1.25, 1.35, 1.50];

        AdaptiveCongestionPolicy? best = null;
        double bestScore = double.NegativeInfinity;

        foreach (double load in loadWeights)
        foreach (double admit in admissionCeilings)
        foreach (double reroute in rerouteThresholds)
        {
            var candidate = new AdaptiveCongestionPolicy
            {
                LoadWeightFactor = load,
                MaxNetworkOccupancyForAdmission = admit,
                RerouteDelayThreshold = reroute
            };

            var network = RoadNetwork.CreateGrid(
                config.GridRows, config.GridCols,
                config.BaseTravelTime, config.BaseEdgeCapacity);

            double score = new TrafficSimulator(network, candidate, config).Run()
                .MeanCompletionsPerTimestep;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best!;
    }

    public static double ThroughputImprovementPercent(
        SimulationMetrics baseline,
        SimulationMetrics adaptive)
    {
        double baseRate = baseline.MeanCompletionsPerTimestep;
        if (baseRate <= 0)
            return 0;

        return 100.0 * (adaptive.MeanCompletionsPerTimestep - baseRate) / baseRate;
    }

    public static IReadOnlyList<DensitySweepResult> RunDensitySweep(
        ICongestionPolicy policy,
        SimulationConfig baseConfig,
        IEnumerable<int> agentCounts)
    {
        var results = new List<DensitySweepResult>();

        foreach (int count in agentCounts)
        {
            var config = new SimulationConfig
            {
                Timesteps = baseConfig.Timesteps,
                AgentCount = count,
                Seed = baseConfig.Seed,
                GridRows = baseConfig.GridRows,
                GridCols = baseConfig.GridCols,
                BaseTravelTime = baseConfig.BaseTravelTime,
                BaseEdgeCapacity = baseConfig.BaseEdgeCapacity
            };

            var network = RoadNetwork.CreateGrid(
                config.GridRows,
                config.GridCols,
                config.BaseTravelTime,
                config.BaseEdgeCapacity);

            var metrics = new TrafficSimulator(network, policy, config).Run();

            results.Add(new DensitySweepResult
            {
                AgentCount = count,
                PolicyName = policy.Name,
                MeanCompletionsPerTimestep = metrics.MeanCompletionsPerTimestep,
                P95TravelTime = metrics.P95TravelTime,
                P50TravelTime = metrics.P50TravelTime,
                MeanActiveAgents = metrics.ActiveAgentsPerTimestep.DefaultIfEmpty(0).Average(),
                TotalCompletions = metrics.TravelTimes.Count
            });
        }

        return results;
    }

    public static string? DetectCongestionTippingPoint(IReadOnlyList<DensitySweepResult> sweep)
    {
        if (sweep.Count < 2)
            return null;

        var ordered = sweep.OrderBy(r => r.AgentCount).ToList();
        for (int i = 1; i < ordered.Count; i++)
        {
            var prev = ordered[i - 1];
            var curr = ordered[i];
            double p95Growth = curr.P95TravelTime - prev.P95TravelTime;
            double densityStep = curr.AgentCount - prev.AgentCount;
            double growthPerAgent = densityStep > 0 ? p95Growth / densityStep : p95Growth;

            // Tipping point: p95 jumps sharply relative to prior step
            if (prev.P95TravelTime > 0 && curr.P95TravelTime / prev.P95TravelTime >= 1.35
                && growthPerAgent >= 0.15)
            {
                return $"~{curr.AgentCount} agents (p95 {prev.P95TravelTime:F1} → {curr.P95TravelTime:F1} timesteps)";
            }
        }

        return null;
    }
}
