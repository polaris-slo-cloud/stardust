using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Links.Ground;
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
    private readonly ILogger<SimulationController> logger;


    private List<Satellite>? satellites = null;
    private List<GroundStation>? groundStations = null;

    private readonly List<Node> all = [];

    private bool autorun;
    private readonly SemaphoreSlim stepEvent = new(0);
    private readonly SemaphoreSlim stepCompleteEvent = new(0);

    public SimulationController(SatelliteConstellationLoader constellationLoader, SimulationConfiguration configuration, ILogger<SimulationController> logger)
    {
        this.constellationLoader = constellationLoader;
        this.configuration = configuration;
        this.logger = logger;

        this.autorun = configuration.StepInterval >= 0;
    }

    #region simulation
    public Task<bool> StartAutorunAsync()
    {
        if (autorun)
        {
            return Task.FromResult(false);
        }

        autorun = true;
        stepEvent.Release();
        return Task.FromResult(true);
    }

    public async Task<bool> StepAsync()
    {
        if (autorun)
        {
            return false;
        }
        stepEvent.Release();
        await stepCompleteEvent.WaitAsync();
        return true;
    }

    public Task StepEndAsync()
    {
        stepCompleteEvent.Release();
        return Task.CompletedTask;
    }

    public Task<bool> StopAutorunAsync()
    {
        if (!autorun)
        {
            return Task.FromResult(false);
        }
        autorun = false;
        return Task.FromResult(true);
    }

    public async Task WaitForStepAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation(autorun ? "Autorun" : "Step");
        if (autorun)
        {
            return;
        }
        await stepEvent.WaitAsync();
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
                groundStations =
                    [
                        new GroundStation("Vienna", 16.3738, 48.2082, new GroundLinkNearestProtocol(await GetAllNodesAsync<Satellite>().ConfigureAwait(false)), new DijkstraRouter()),
                        new GroundStation("Reykjavik", -21.8277, 64.1283, new GroundLinkNearestProtocol(await GetAllNodesAsync<Satellite>().ConfigureAwait(false)), new DijkstraRouter()),
                        new GroundStation("New York", -74.0060, 40.7128, new GroundLinkNearestProtocol(await GetAllNodesAsync<Satellite>().ConfigureAwait(false)), new DijkstraRouter()),
                        new GroundStation("Sydney", 151.2093, -33.8688, new GroundLinkNearestProtocol(await GetAllNodesAsync<Satellite>().ConfigureAwait(false)), new DijkstraRouter()),
                        new GroundStation("Buenos Aires", -58.3816, -34.6037, new GroundLinkNearestProtocol(await GetAllNodesAsync<Satellite>().ConfigureAwait(false)), new DijkstraRouter()),
                    ];
                all.AddRange(groundStations);
            }
            return groundStations.Cast<T>();
        }
        return all.Cast<T>();
    }
    private Task<IEnumerable<T>> GetAllNodesInternalAsync<T>(ComputingType computingType, IEnumerable<T> list) where T : Node
    {
        return Task.FromResult(list.Where(n => n.Computing.Type == computingType));
    }
    #endregion
}
