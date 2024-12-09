using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Links;
using StardustLibrary.Routing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public class SimulationController : ISimulationController
{
    private readonly SatelliteConstellationLoader constellationLoader;
    private readonly SimulationConfiguration configuration;
    private readonly RouterBuilder routerBuilder;
    private readonly ComputingBuilder computingBuilder;
    private readonly ILogger<SimulationController> logger;


    private List<Satellite>? satellites = null;
    private List<GroundStation>? groundStations = null;

    private readonly List<Node> all = [];

    private readonly SemaphoreSlim stepEvent = new(0);
    private readonly SemaphoreSlim stepCompleteEvent = new(0);

    public bool Autorun { get; private set; }

    public SimulationController(SatelliteConstellationLoader constellationLoader, SimulationConfiguration configuration, RouterBuilder routerBuilder, ComputingBuilder computingBuilder, ILogger<SimulationController> logger)
    {
        this.constellationLoader = constellationLoader;
        this.configuration = configuration;
        this.routerBuilder = routerBuilder;
        this.computingBuilder = computingBuilder;
        this.logger = logger;

        this.Autorun = configuration.StepInterval >= 0;

        routerBuilder.Nodes = all;
    }

    #region simulation
    public Task<bool> StartAutorunAsync()
    {
        if (Autorun)
        {
            return Task.FromResult(false);
        }

        Autorun = true;
        stepEvent.Release();
        return Task.FromResult(true);
    }

    public async Task<bool> StepAsync()
    {
        if (Autorun)
        {
            return false;
        }
        stepEvent.Release();
        await stepCompleteEvent.WaitAsync().ConfigureAwait(false);
        return true;
    }

    public Task WaitForStepEndAsync()
    {
        stepCompleteEvent.Release();
        return Task.CompletedTask;
    }

    public Task<bool> StopAutorunAsync()
    {
        if (!Autorun)
        {
            return Task.FromResult(false);
        }
        Autorun = false;
        return Task.FromResult(true);
    }

    public async Task WaitForStepAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation(Autorun ? "Autorun" : "Step");
        if (Autorun)
        {
            return;
        }
        await stepEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region API
    public Task<List<Node>> GetAllNodesAsync()
    {
        return Task.FromResult(all.ToList());
    }

    public async Task<List<Node>> GetAllNodesAsync(ComputingType computingType)
    {
        var list = await GetAllNodesInternalAsync(computingType, all).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<List<T>> GetAllNodesAsync<T>() where T : Node
    {
        var list = await GetAllNodesInternalAsync<T>().ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<List<T>> GetAllNodesAsync<T>(ComputingType computingType) where T : Node
    {
        var list = await GetAllNodesInternalAsync<T>().ConfigureAwait(false);
        list = await GetAllNodesInternalAsync(computingType, list).ConfigureAwait(false);
        return list.ToList();
    }

    private async Task<IEnumerable<T>> GetAllNodesInternalAsync<T>() where T : Node
    {
        if (typeof(Satellite).IsAssignableFrom(typeof(T)))
        {
            if (satellites == null)
            {
                satellites = await constellationLoader.LoadSatelliteConstellation(configuration.SatelliteDataSource, configuration.SatelliteDataSourceType).ConfigureAwait(false);
                all.AddRange(satellites);
            }
            return satellites.Cast<T>();
        }
        if (typeof(GroundStation).IsAssignableFrom(typeof(T)))
        {
            if (groundStations == null)
            {
                if (satellites == null)
                {
                    await GetAllNodesInternalAsync<Satellite>();
                }
                groundStations =
                    [
                        new GroundStation("Vienna", 16.3738, 48.2082, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                        new GroundStation("Bratislava", 17.1077, 48.1486, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                        new GroundStation("Reykjavik", -21.8277, 64.1283, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                        new GroundStation("New York", -74.0060, 40.7128, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                        new GroundStation("Sydney", 151.2093, -33.8688, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                        new GroundStation("Buenos Aires", -58.3816, -34.6037, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    ];
                all.AddRange(groundStations);
            }
            return groundStations.Cast<T>();
        }
        return all.Cast<T>();
    }
    private static Task<IEnumerable<T>> GetAllNodesInternalAsync<T>(ComputingType computingType, IEnumerable<T> list) where T : Node
    {
        return Task.FromResult(list.Where(n => n.Computing.Type == computingType));
    }
    #endregion
}
