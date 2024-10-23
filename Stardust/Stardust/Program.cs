using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using Stardust.Abstraction.Simulation;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Routing;
using StardustLibrary.Simulation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO load config from config file
builder.Services.AddSingleton(new SimulationConfiguration
{
    //StepInterval = 5,
    //StepLength = 300,
    StepInterval = -1,
    StepMultiplier = 10,
    SatelliteDataSource = "starlink_6000.tle",
    SatelliteDataSourceType = "tle",
    UsePreRouteCalc = false,
});
builder.Services.AddSingleton(new InterSatelliteLinkConfig
{
    Neighbours = 4,
    Protocol = "other_mst_loop"
});
builder.Services.AddSingleton(new RouterConfig
{
    Protocol = "a-star"
});

builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<ISimulationController, SimulationController>();
builder.Services.AddSingleton<SatelliteConstellationLoader>();
builder.Services.AddSingleton<TleSatelliteConstellationLoader>();

builder.Services.AddSingleton<SatelliteBuilder>();
builder.Services.AddSingleton<RouterBuilder>();

builder.Services.AddHostedService<SimulationService>();
builder.Services.AddHostedService<SatelliteConstellationLoaderService>();

using var host = builder.Build();
var logger = host.Services.GetService<ILogger<Program>>() ?? throw new ApplicationException("Cannot get logger");
var simulationController = host.Services.GetRequiredService<ISimulationController>()!;


//await host.RunAsync(); return;

#pragma warning disable CS0162 // Unreachable code detected
await host.StartAsync();

var random = new Random();
var nodes = await simulationController.GetAllNodesAsync();
var groundStations = await simulationController.GetAllNodesAsync<GroundStation>();

await simulationController.StepAsync();


int steps = 0;
var source = groundStations[random.Next(0, groundStations.Count - 1)];
var targets = groundStations.Where(g => g != source);
var dict = groundStations.ToDictionary(g => g, _ => 0D);
while (++steps > 0)
{
    if (steps % 1000 == 0)
    {
        foreach (var target in targets)
        {
            dict[target] = dict[target] / steps;
            steps = 0;
        }
    }

    await simulationController.StepAsync();
    foreach (var target in targets)
    {
        var sw = Stopwatch.StartNew();
        await source.Router.Route(target, new Workload());
        var stop = sw.ElapsedMilliseconds;
        dict[target] = dict[target] + stop;
        logger.LogInformation("Route from {0} to {1} took {2}ms avg {3}ms", source.Name, target.Name, sw.ElapsedMilliseconds, dict[target] / steps);
    }
    await Task.Delay(1000);
}

await host.StopAsync();
await host.WaitForShutdownAsync();
#pragma warning restore CS0162 // Unreachable code detected