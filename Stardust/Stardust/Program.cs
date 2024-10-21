using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Simulation;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Simulation;
using System;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO load config from config file
builder.Services.AddSingleton(new SimulationConfiguration
{
    StepInterval = 15,
    StepLength = 300,
    //StepInterval = -1,
    //StepMultiplier = 10,
    SatelliteDataSource = "starlink.tle",
    SatelliteDataSourceType = "tle",
});
builder.Services.AddSingleton(new InterSatelliteLinkConfig
{
    Neighbours = 4,
    Protocol = "mst"
});

builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<ISimulationController, SimulationController>();
builder.Services.AddSingleton<SatelliteConstellationLoader>();
builder.Services.AddSingleton<TleSatelliteConstellationLoader>();

builder.Services.AddHostedService<SimulationService>();
builder.Services.AddHostedService<SatelliteConstellationLoaderService>();

using var host = builder.Build();
var logger = host.Services.GetService<ILogger<Program>>() ?? throw new ApplicationException("Cannot get logger");
var simulationController = host.Services.GetRequiredService<ISimulationController>()!;


await host.RunAsync(); return;

#pragma warning disable CS0162 // Unreachable code detected
await host.StartAsync();

var random = new Random();
var nodes = await simulationController.GetAllNodesAsync();

await simulationController.StepAsync();
await simulationController.StepAsync();

var source = nodes[random.Next(0, nodes.Count - 1)];
var target = nodes[random.Next(0, nodes.Count - 1)];

await source.Router.Route(target, new Workload());

await host.WaitForShutdownAsync();
#pragma warning restore CS0162 // Unreachable code detected