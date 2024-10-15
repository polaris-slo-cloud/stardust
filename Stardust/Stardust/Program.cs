using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Node.Networking;
using StardustLibrary.Simulation;
using System;
using System.Threading.Tasks;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO load config from config file
builder.Services.AddSingleton(new SimulationConfiguration
{
    //StepInterval = 1,
    //StepLength = 10,
    StepMultiplier = 10,
    SatelliteDataSource = "starlink.tle",
    SatelliteDataSourceType = "tle",
});
builder.Services.AddSingleton(new InterSatelliteLinkConfig
{
    Neighbours = 4,
    Protocol = "nearest"
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

await host.RunAsync();


/*
await host.StartAsync();


//await simulationController.StopAutorunAsync();

await simulationController.StepAsync();
await Task.Delay(10_000);
await simulationController.StepAsync();

await Task.Delay(30_000);
await simulationController.StartAutorunAsync();

await host.WaitForShutdownAsync();
*/