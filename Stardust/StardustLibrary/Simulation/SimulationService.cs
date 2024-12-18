using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Links;
using StardustLibrary.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public class SimulationService : IHostedService, ISimulationController
{
    private readonly SimulationConfiguration simulationConfiguration;
    private readonly SatelliteConstellationLoader constellationLoader;
    private readonly RouterBuilder routerBuilder;
    private readonly ComputingBuilder computingBuilder;
    private readonly ILogger<SimulationService> logger;
    private readonly ParallelOptions _parallelOptions;

    private readonly Object lockObject = new();

    private Task runningSimulationTask = Task.CompletedTask;
    private DateTime simTime;

    private readonly List<Node> all = [];
    private List<Satellite> satellites = [];
    private List<GroundStation> groundStations = [];

    public bool Autorun { get; private set; }
    public DateTime StartTime { get; }

    public SimulationService(SimulationConfiguration simulationConfiguration, SatelliteConstellationLoader constellationLoader, RouterBuilder routerBuilder, ComputingBuilder computingBuilder, ILogger<SimulationService> logger)
    {
        this.simulationConfiguration = simulationConfiguration;
        this.constellationLoader = constellationLoader;
        this.routerBuilder = routerBuilder;
        this.computingBuilder = computingBuilder;
        this.logger = logger;
        this._parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = simulationConfiguration.MaxCpuCores,
        };

        this.Autorun = simulationConfiguration.StepInterval >= 0;
        this.StartTime = simTime = simulationConfiguration.SimulationStartTime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Staring simulation ...");    

        satellites = await GetAllNodesAsync<Satellite>();
        groundStations = await GetAllNodesAsync<GroundStation>();
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping simulation ...");
        Autorun = false;
        return runningSimulationTask;
    }

    private async Task RunSimulationStep(Func<DateTime, DateTime> getSimTime, CancellationToken cancellationToken = default)
    {
        double delta = 0;
        Stopwatch sw = Stopwatch.StartNew();
        do
        {
            simTime = getSimTime(simTime);
            logger.LogInformation("Simulation time is {0}", simTime.ToString());

            // Update all node positions
            Parallel.ForEach(all, _parallelOptions, async (n) => await n.UpdatePosition(simTime).ConfigureAwait(false));
            logger.LogInformation("UpdatePosition after {0}ms", sw.ElapsedMilliseconds);

            // Update ISL and Ground Links
            Parallel.ForEach(satellites, _parallelOptions, async (s) => await s.InterSatelliteLinkProtocol.UpdateLinks().ConfigureAwait(false));
            Parallel.ForEach(groundStations, _parallelOptions, async (g) => await g.GroundSatelliteLinkProtocol.UpdateLink().ConfigureAwait(false));
            logger.LogInformation("UpdateLinks after {0}ms", sw.ElapsedMilliseconds);

            // if pre routing is enabled do pre routing
            if (simulationConfiguration.UsePreRouteCalc)
            {
                Parallel.ForEach(satellites, _parallelOptions, async (s) => await s.Router.CalculateRoutingTableAsync().ConfigureAwait(false));
                logger.LogInformation("CalculateRoutingTableAsync after {0}ms", sw.ElapsedMilliseconds);
            }

            if (Autorun && sw.Elapsed.Seconds < simulationConfiguration.StepInterval)
            {
                int wait = (int)((simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) + delta);
                logger.LogInformation("wait {0}ms", wait);
                if (wait > 0)
                {
                    await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
                }
                delta = (delta + simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) / 2;
            }
            else
            {
                delta = sw.ElapsedMilliseconds / 3;
                await Task.Delay((int)(delta), cancellationToken).ConfigureAwait(false);
            }
            logger.LogInformation("Round took {0}ms; delta: {1}ms", sw.ElapsedMilliseconds, delta);
            sw.Restart();
        } while (Autorun);
    }

    #region simulation
    public Task StartAutorunAsync()
    {
        lock (lockObject)
        {
            if (Autorun)
            {
                return Task.CompletedTask;
            }

            runningSimulationTask.ConfigureAwait(false).GetAwaiter().GetResult();
            Autorun = true;
            return runningSimulationTask = RunSimulationStep((prev) => prev.AddSeconds((double)((prev - DateTime.Now).Seconds * simulationConfiguration.StepMultiplier!)));
        }
    }

    public Task StepAsync(DateTime newSimTime, CancellationToken cancellationToken)
    {
        lock (lockObject)
        {
            if (Autorun)
            {
                return Task.CompletedTask;
            }

            runningSimulationTask.ConfigureAwait(false).GetAwaiter().GetResult();
            return runningSimulationTask = RunSimulationStep((_) => newSimTime, cancellationToken);
        }
    }

    public Task StepAsync(double seconds, CancellationToken cancellationToken)
    {
        lock (lockObject)
        {
            if (Autorun)
            {
                return Task.CompletedTask;
            }

            runningSimulationTask.ConfigureAwait(false).GetAwaiter().GetResult();
            return runningSimulationTask = RunSimulationStep((prev) => prev.AddSeconds(seconds), cancellationToken);
        }
    }

    public Task StopAutorunAsync()
    {
        if (!Autorun)
        {
            return Task.CompletedTask;
        }
        Autorun = false;
        return runningSimulationTask;
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
            if (satellites.Count == 0)
            {
                satellites = await constellationLoader.LoadSatelliteConstellation(simulationConfiguration.SatelliteDataSource, simulationConfiguration.SatelliteDataSourceType).ConfigureAwait(false);
                all.AddRange(satellites);
            }
            return satellites.Cast<T>();
        }
        if (typeof(GroundStation).IsAssignableFrom(typeof(T)))
        {
            if (groundStations.Count == 0)
            {
                if (satellites.Count == 0)
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
