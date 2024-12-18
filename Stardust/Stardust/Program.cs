using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stardust;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Routing;
using Stardust.Abstraction.Simulation;
using Stardust.HttpService;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Deployment;
using StardustLibrary.Routing;
using StardustLibrary.Simulation;
using System;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO load config from config file
builder.Services.AddSingleton(new SimulationConfiguration
{
    //StepInterval = 30,
    //StepLength = 300,
    StepInterval = -1,
    StepMultiplier = 10,
    SatelliteDataSource = "starlink_6000.tle",
    SatelliteDataSourceType = "tle",
    UsePreRouteCalc = true,
    MaxCpuCores = 30,
    SimulationStartTime = new DateTime(2024, 1, 1)
});
builder.Services.AddSingleton(new InterSatelliteLinkConfig
{
    Neighbours = 4,
    Protocol = "mst_smart_loop"
});
builder.Services.AddSingleton(new RouterConfig
{
    Protocol = "dijkstra" // dijkstra a-star
});
builder.Services.AddSingleton(new ComputingConfiguration
{
    Configurations = [
        new Computing(512, 4096, ComputingType.Edge),
        new Computing(1024, 32768, ComputingType.Cloud),
    ]
});

builder.Services.AddSingleton<ISimulationController, SimulationService>();
builder.Services.AddSingleton<SatelliteConstellationLoader>();
builder.Services.AddSingleton<TleSatelliteConstellationLoader>();

builder.Services.AddSingleton<SatelliteBuilder>();
builder.Services.AddSingleton<RouterBuilder>();

builder.Services.AddSingleton<IDeploymentOrchestrator, DefaultDeploymentOrchestrator>();
builder.Services.AddSingleton<TaskOrchestrator>();
builder.Services.AddSingleton<IDeploymentOrchestrator>(serviceProvider => serviceProvider.GetRequiredService<TaskOrchestrator>());
builder.Services.AddSingleton<IDeploymentOrchestrator, WorkflowOrchestrator>();
builder.Services.AddSingleton<DeploymentOrchestratorResolver>();
builder.Services.AddSingleton<DeploymentOrchestrator>();

builder.Services.AddTransient<ComputingBuilder>();

builder.Services.AddHostedService<SatelliteConstellationLoaderService>();
builder.Services.AddHostedService<PaperWorkflowTestService>();
//builder.Services.AddHostedService<PaperTaskTestService>();
//builder.Services.AddHostedService<StatService>();
//builder.Services.AddHostedService<HttpService>();
//builder.Services.AddHostedService<SendRequestsService>();

using var host = builder.Build();
await host.RunAsync();
