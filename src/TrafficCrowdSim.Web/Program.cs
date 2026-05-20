using System.Net;
using System.Net.Sockets;
using TrafficCrowdSim.Core.Control;
using TrafficCrowdSim.Core.Network;
using TrafficCrowdSim.Core.Simulation;
using TrafficCrowdSim.Core.Visualization;

static int FindAvailablePort(int preferred)
{
    if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int fromEnv))
        preferred = fromEnv;

    for (int port = preferred; port < preferred + 20; port++)
    {
        if (IsPortFree(port))
            return port;
    }

    var ephemeral = new TcpListener(IPAddress.Loopback, 0);
    ephemeral.Start();
    int assigned = ((IPEndPoint)ephemeral.LocalEndpoint).Port;
    ephemeral.Stop();
    return assigned;
}

static bool IsPortFree(int port)
{
    try
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/default-config", () => new SimulationConfig
{
    Timesteps = 180,
    AgentCount = 120,
    Seed = 42,
    GridRows = 6,
    GridCols = 6,
    BaseEdgeCapacity = 3
});

app.MapGet("/api/simulate", (
    string? policy,
    int? timesteps,
    int? agents,
    int? seed,
    int? rows,
    int? cols,
    int? capacity) =>
{
    var config = new SimulationConfig
    {
        Timesteps = timesteps ?? 180,
        AgentCount = agents ?? 120,
        Seed = seed ?? 42,
        GridRows = rows ?? 6,
        GridCols = cols ?? 6,
        BaseEdgeCapacity = capacity ?? 3
    };

    var network = RoadNetwork.CreateGrid(
        config.GridRows, config.GridCols, config.BaseTravelTime, config.BaseEdgeCapacity);

    ICongestionPolicy selected = policy?.ToLowerInvariant() switch
    {
        "baseline" => new BaselinePolicy(),
        _ => new AdaptiveCongestionPolicy()
    };

    var recording = SimulationRecorder.Record(network, selected, config, snapshotEvery: 1);
    return Results.Json(recording);
});

const int preferredPort = 5180;
int port = FindAvailablePort(preferredPort);
var url = $"http://localhost:{port}";
Console.WriteLine($"Traffic visualizer: {url}");
if (port != preferredPort)
    Console.WriteLine($"(Port {preferredPort} is already in use — close the other instance or use {url})");

app.Run(url);
