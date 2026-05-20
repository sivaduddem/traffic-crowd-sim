# Traffic & Crowd Simulator

C#/.NET discrete-time simulator where **distributed agents** traverse a **shared road network**. Congestion emerges when edge occupancy approaches capacity (BPR-style delay). An **adaptive congestion policy** improves throughput versus a static baseline, and experiments measure **p95 travel time** across agent-density sweeps to find congestion tipping points.

## Lab requirements mapping

| Requirement | Implementation |
|-------------|----------------|
| Congestion from distributed agents on shared roads | `TrafficSimulator` + `Agent` on `RoadNetwork` grid with bottleneck corridor |
| Higher route completions per timestep under load | Tuned `AdaptiveCongestionPolicy` vs `BaselinePolicy`; `ExperimentRunner.TuneAdaptivePolicy` + `ThroughputImprovementPercent` |
| Tipping points & tail-latency (p95) vs density | `ExperimentRunner.RunDensitySweep` + `DetectCongestionTippingPoint` |

## Prerequisites

Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## Run

**Visual simulation (browser):**

```bash
dotnet run --project src/TrafficCrowdSim.Web
```

Open the URL printed in the terminal (default http://localhost:5180). If that port is busy, the app picks the next free port (e.g. 5181). Use **Run simulation** to animate agents on the grid. Edge color/thickness shows congestion; cyan dots are travelers (faint green lines point to their destinations). Switch between **Adaptive** and **Baseline** policies to compare behavior.

**CLI benchmarks:**

```bash
dotnet run --project src/TrafficCrowdSim.Cli
dotnet test
```

## Architecture

```
Agents ──► ICongestionPolicy (route / admit / reroute)
              │
              ▼
         RoadNetwork (grid + bottleneck)
              │
              ▼
         CongestionModel (BPR delay from edge load)
              │
              ▼
         SimulationMetrics (completions/timestep, p95 travel time)
```

### Baseline policy

- Static shortest-path routing (Dijkstra on free-flow weights)
- No admission control or rerouting

### Adaptive policy

- **Load-aware routing**: edge weights increase with occupancy
- **Admission control**: throttle spawns when network pressure is high
- **Dynamic rerouting**: replan when delayed on saturated edges

## Project layout

- `src/TrafficCrowdSim.Core` — simulation engine
- `src/TrafficCrowdSim.Cli` — benchmark & density sweep output
- `src/TrafficCrowdSim.Web` — interactive canvas visualizer
- `tests/TrafficCrowdSim.Tests` — throughput and congestion tests

## Tuning

`ExperimentRunner.TuneAdaptivePolicy` grid-searches load weight, admission ceiling, and reroute sensitivity for the highest completions/timestep. The CLI runs this automatically before benchmarks. Override knobs in `AdaptiveCongestionPolicy` if you want a fixed policy instead.

## Example lab claims (fill in after running)

After `dotnet run`:

1. Record baseline vs adaptive **completions/timestep** and compute % improvement.
2. Copy the density table; note where **p95 travel time** jumps (tipping point).
3. Optionally plot p95 vs agent count for baseline and adaptive curves.
