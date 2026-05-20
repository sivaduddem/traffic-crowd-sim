using TrafficCrowdSim.Core.Control;
using TrafficCrowdSim.Core.Experiments;
using TrafficCrowdSim.Core.Simulation;

var config = new SimulationConfig
{
    Timesteps = 100,
    AgentCount = 280,
    Seed = 42,
    GridRows = 6,
    GridCols = 6,
    BaseEdgeCapacity = 2
};

Console.WriteLine("=== Traffic & Crowd Simulator ===\n");

// 1) Throughput comparison: baseline vs auto-tuned adaptive policy
var tuned = ExperimentRunner.TuneAdaptivePolicy(config);
var (baseline, adaptive) = ExperimentRunner.ComparePolicies(config, tuned);
double improvement = ExperimentRunner.ThroughputImprovementPercent(baseline, adaptive);

Console.WriteLine("--- Congestion control throughput (lab metric) ---");
Console.WriteLine($"Tuned adaptive: load={tuned.LoadWeightFactor}, admit≤{tuned.MaxNetworkOccupancyForAdmission:P0}, reroute×{tuned.RerouteDelayThreshold}");
Console.WriteLine($"Baseline:  {baseline.MeanCompletionsPerTimestep:F3} completions/timestep, " +
                  $"p95 travel={baseline.P95TravelTime:F1}");
Console.WriteLine($"Adaptive:  {adaptive.MeanCompletionsPerTimestep:F3} completions/timestep, " +
                  $"p95 travel={adaptive.P95TravelTime:F1}");
Console.WriteLine($"Improvement: {improvement:+#.#;-#.#;0}% route completions per timestep\n");

// 2) Density sweep: tipping points and tail latency
int[] densities = [24, 48, 72, 96, 120, 144];
var baselineSweep = ExperimentRunner.RunDensitySweep(new BaselinePolicy(), config, densities);
var adaptiveSweep = ExperimentRunner.RunDensitySweep(tuned, config, densities);

Console.WriteLine("--- Density sweep: p95 travel time (tail latency) ---");
Console.WriteLine($"{"Agents",8} {"Baseline p95",14} {"Adaptive p95",14} {"Baseline μ",12} {"Adaptive μ",12}");
foreach (int n in densities)
{
    var b = baselineSweep.First(r => r.AgentCount == n);
    var a = adaptiveSweep.First(r => r.AgentCount == n);
    Console.WriteLine($"{n,8} {b.P95TravelTime,14:F1} {a.P95TravelTime,14:F1} " +
                      $"{b.MeanCompletionsPerTimestep,12:F3} {a.MeanCompletionsPerTimestep,12:F3}");
}

string? tipping = ExperimentRunner.DetectCongestionTippingPoint(baselineSweep);
Console.WriteLine();
if (tipping is not null)
    Console.WriteLine($"Congestion tipping point (baseline): {tipping}");
else
    Console.WriteLine("Congestion tipping point: increase agent counts or timesteps to observe sharper p95 growth.");

Console.WriteLine("\nDone. See README for architecture and lab write-up notes.");
