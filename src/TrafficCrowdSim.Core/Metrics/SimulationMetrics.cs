namespace TrafficCrowdSim.Core.Metrics;

public sealed class SimulationMetrics
{
    public List<int> CompletionsPerTimestep { get; } = new();
    public List<int> TravelTimes { get; } = new();
    public List<double> ActiveAgentsPerTimestep { get; } = new();
    public List<double> MeanEdgeOccupancyPerTimestep { get; } = new();

    public void RecordTimestep(int completions, double activeAgents, double meanEdgeOccupancy)
    {
        CompletionsPerTimestep.Add(completions);
        ActiveAgentsPerTimestep.Add(activeAgents);
        MeanEdgeOccupancyPerTimestep.Add(meanEdgeOccupancy);
    }

    public void RecordCompletion(int travelTime) => TravelTimes.Add(travelTime);

    public double MeanCompletionsPerTimestep =>
        CompletionsPerTimestep.Count == 0 ? 0 : CompletionsPerTimestep.Average();

    public double PercentileTravelTime(double percentile)
    {
        if (TravelTimes.Count == 0)
            return 0;

        var sorted = TravelTimes.OrderBy(t => t).ToList();
        double rank = percentile * (sorted.Count - 1);
        int low = (int)Math.Floor(rank);
        int high = (int)Math.Ceiling(rank);
        if (low == high)
            return sorted[low];

        double weight = rank - low;
        return sorted[low] * (1 - weight) + sorted[high] * weight;
    }

    public double P95TravelTime => PercentileTravelTime(0.95);
    public double P50TravelTime => PercentileTravelTime(0.50);
}

public sealed class DensitySweepResult
{
    public required int AgentCount { get; init; }
    public required string PolicyName { get; init; }
    public required double MeanCompletionsPerTimestep { get; init; }
    public required double P95TravelTime { get; init; }
    public required double P50TravelTime { get; init; }
    public required double MeanActiveAgents { get; init; }
    public required int TotalCompletions { get; init; }
}
