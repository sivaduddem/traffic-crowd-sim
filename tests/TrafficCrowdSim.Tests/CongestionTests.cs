using Xunit;
using TrafficCrowdSim.Core.Congestion;
using TrafficCrowdSim.Core.Experiments;
using TrafficCrowdSim.Core.Simulation;

namespace TrafficCrowdSim.Tests;

public class CongestionTests
{
    [Fact]
    public void Travel_time_multiplier_grows_with_occupancy()
    {
        double low = CongestionModel.TravelTimeMultiplier(0.3);
        double high = CongestionModel.TravelTimeMultiplier(1.2);
        Assert.True(high > low);
    }

    [Fact]
    public void Adaptive_policy_improves_throughput_vs_baseline()
    {
        var config = new SimulationConfig
        {
            Timesteps = 200,
            AgentCount = 96,
            Seed = 7,
            GridRows = 6,
            GridCols = 6
        };

        var tuned = ExperimentRunner.TuneAdaptivePolicy(config);
        var (baseline, adaptive) = ExperimentRunner.ComparePolicies(config, tuned);

        Assert.True(adaptive.MeanCompletionsPerTimestep > baseline.MeanCompletionsPerTimestep);
        Assert.True(ExperimentRunner.ThroughputImprovementPercent(baseline, adaptive) > 0);
    }

    [Fact]
    public void Density_sweep_shows_p95_growth_under_load()
    {
        var config = new SimulationConfig { Timesteps = 180, Seed = 11, GridRows = 5, GridCols = 5 };
        var sweep = ExperimentRunner.RunDensitySweep(
            new Control.BaselinePolicy(),
            config,
            [30, 60, 90]);

        Assert.True(sweep[^1].P95TravelTime >= sweep[0].P95TravelTime);
    }
}
