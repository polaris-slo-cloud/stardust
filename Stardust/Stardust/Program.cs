using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Simulation;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO load config from config file
builder.Services.AddSingleton(new SimulationConfiguration
{
    StepInterval = 1,
    StepLength = 10,
    SatelliteDataSource = "starlink.tle",
    SatelliteDataSourceType = "tle",
});

builder.Services.AddSingleton<SatelliteConstellationLoader>();
builder.Services.AddSingleton<TleSatelliteConstellationLoader>();

builder.Services.AddHostedService<SimulationService>();
builder.Services.AddHostedService<SatelliteConstellationLoaderService>();

using IHost host = builder.Build();

await host.RunAsync();
