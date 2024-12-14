using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Simulation;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class SatelliteConstellationLoaderService : BackgroundService
{
    private readonly SimulationService? simulationServcie;

    public SatelliteConstellationLoaderService(System.IServiceProvider serviceProvider, ISimulationController simulationController)
    {
        _ = serviceProvider.GetService<TleSatelliteConstellationLoader>();
        if (simulationController is SimulationService simulationService)
        {
            this.simulationServcie = simulationService;
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        simulationServcie?.StartAsync(cancellationToken).Wait(cancellationToken);
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
