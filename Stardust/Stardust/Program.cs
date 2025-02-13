using Microsoft.Extensions.Configuration;
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
using System.Collections.Generic;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json");

builder.Services.Configure<SimulationConfiguration>(builder.Configuration.GetSection("SimulationConfiguration"));
builder.Services.Configure<InterSatelliteLinkConfig>(builder.Configuration.GetSection("InterSatelliteLinkConfig"));
builder.Services.Configure<RouterConfig>(builder.Configuration.GetSection("RouterConfig"));
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
