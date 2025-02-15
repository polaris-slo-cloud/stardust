using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public SimulationService(IOptions<SimulationConfiguration> simulationConfiguration, SatelliteConstellationLoader constellationLoader, RouterBuilder routerBuilder, ComputingBuilder computingBuilder, ILogger<SimulationService> logger)
    {
        this.simulationConfiguration = simulationConfiguration.Value;
        this.constellationLoader = constellationLoader;
        this.routerBuilder = routerBuilder;
        this.computingBuilder = computingBuilder;
        this.logger = logger;
        this._parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = this.simulationConfiguration.MaxCpuCores,
        };

        this.Autorun = this.simulationConfiguration.StepInterval >= 0;
        this.StartTime = simTime = this.simulationConfiguration.SimulationStartTime;
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
                    new GroundStation("Graz", 15.4409, 47.0707, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Vienna", 16.3738, 48.2082, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Ljubljana", 14.5058, 46.0569, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Zagreb", 15.9819, 45.8150, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Bratislava", 17.1077, 48.1486, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Havlickuv Brod", 15.5808, 49.6065, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Prague", 14.4378, 50.0755, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Brno", 16.6068, 49.1951, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Praha", 14.4378, 50.0755, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Zurich", 8.5417, 47.3769, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Zlin", 17.6668, 49.2245, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Rtyne v Podkrkonosi", 16.0586, 50.5165, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Budapest", 19.0402, 47.4979, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Ostrava", 18.2926, 49.8347, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Frankfurt", 8.6821, 50.1109, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Katowice", 19.0238, 50.2649, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Milan", 9.1900, 45.4642, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Belgrade", 20.4573, 44.8176, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Stargard", 15.0476, 53.3366, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Brussels", 4.3517, 50.8503, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Warsaw", 21.0122, 52.2297, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Amsterdam", 4.9041, 52.3676, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Paris", 2.3522, 48.8566, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Skopje", 21.4275, 41.9981, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Lomza", 22.0898, 53.1781, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Gravelines", 2.1206, 50.9869, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Copenhagen", 12.5683, 55.6761, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Palermo", 13.3613, 38.1157, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Bucharest", 26.1025, 44.4268, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("London Docklands", -0.0178, 51.5064, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Thessaloniki", 22.9375, 40.6401, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("London", -0.1276, 51.5074, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Vilnius", 25.2797, 54.6872, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Chisinau", 28.8254, 47.0105, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Manchester", -2.2426, 53.4808, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Riga", 24.1052, 56.9496, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Stockholm", 18.0686, 59.3293, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Oslo", 10.7522, 59.9139, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Tallinn", 24.7536, 59.4370, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Madrid", -3.7038, 40.4168, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Ireland", -7.6921, 53.4129, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Merefa", 36.0448, 49.8216, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("St. Petersburg", 30.3158, 59.9343, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Seville", -5.9845, 37.3891, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Limassol", 33.0456, 34.6751, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Tel Aviv", 34.7818, 32.0853, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Hafnarfjordur", -21.9531, 64.0676, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Riyadh", 46.7219, 24.7136, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Dubai", 55.2708, 25.2048, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Delhi", 77.1025, 28.7041, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Nashik", 73.7898, 19.9975, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Mumbai", 72.8777, 19.0760, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Montreal", -73.5673, 45.5017, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("New York", -74.0060, 40.7128, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Hyderabad", 78.4867, 17.3850, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Toronto", -79.3832, 43.6532, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Ashburn", -77.4874, 39.0438, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Washington", -77.0369, 38.9072, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Los Angeles", -118.2437, 34.0522, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Bangalore", 77.5946, 12.9716, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Dhaka", 90.4125, 23.8103, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("CUMILLA", 91.1916, 23.4571, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Chicago", -87.6298, 41.8781, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Bogota", -74.0721, 4.7110, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Miami", -80.1918, 25.7617, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Johannesburg", 28.0473, -26.2041, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Vancouver", -123.1216, 49.2827, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Seoul", 126.9780, 37.5665, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Seattle", -122.3321, 47.6062, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Dallas", -96.7970, 32.7767, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Salt Lake City", -111.8910, 40.7608, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Hong Kong", 114.1694, 22.3193, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Kwai Chung", 114.1328, 22.3626, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Osaka", 135.5022, 34.6937, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Tokyo", 139.6917, 35.6895, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Sao Paulo", -46.6333, -23.5505, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Singapore", 103.8198, 1.3521, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Jakarta", 106.8456, -6.2088, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Santiago", -70.6483, -33.4489, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Vina del Mar", -71.5500, -33.0245, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Honolulu", -157.8583, 21.3069, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Dili", 125.5795, -8.5569, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Melbourne", 144.9631, -37.8136, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Sydney", 151.2093, -33.8688, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build()),
                    new GroundStation("Mexico City", -99.1332, 19.4326, new GroundSatelliteNearestProtocol(satellites), routerBuilder.Build(), computingBuilder.WithComputingType(ComputingType.Cloud).Build())
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
