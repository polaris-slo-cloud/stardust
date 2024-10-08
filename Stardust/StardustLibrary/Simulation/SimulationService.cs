using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Node;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public class SimulationService : BackgroundService
{
    private readonly SatelliteConstellationLoader constellationLoader;
    private readonly SimulationConfiguration simulationConfiguration;
    private readonly ILogger<SimulationService> logger;

    private DateTime startTime;
    private DateTime simTime;

    private List<Satellite> satellites = [];
    private List<GroundStation> groundStations = [];

    public SimulationService(SatelliteConstellationLoader constellationLoader, SimulationConfiguration simulationConfiguration, ILogger<SimulationService> logger)
    {
        this.constellationLoader = constellationLoader;
        this.simulationConfiguration = simulationConfiguration;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Staring simulation ...");

        satellites = await constellationLoader.LoadSatelliteConstellation(simulationConfiguration.SatelliteDataSource, simulationConfiguration.SatelliteDataSourceType);
        groundStations =
        [
            new GroundStation("Vienna", 16.3738, 48.2082),
            new GroundStation("Reykjavik", -21.8277, 64.1283),
            new GroundStation("New York", -74.0060, 40.7128),
            new GroundStation("Sydney", 151.2093, -33.8688),
            new GroundStation("Buenos Aires", -58.3816, -34.6037),
        ];

        startTime = simTime = DateTime.UtcNow;

        Stopwatch sw = Stopwatch.StartNew();
        while (!stoppingToken.IsCancellationRequested)
        {
            simTime = simTime.AddSeconds(simulationConfiguration.StepLength);

            sw.Restart();
            // Update all satellite positions
            foreach (var satellite in satellites)
            {
                await satellite.UpdatePosition(simTime);
            }

            //Parallel.ForEach(Satellites, new ParallelOptions { MaxDegreeOfParallelism = 8 }, Satellite => Satellite.UpdatePosition(secondsElapsed));

            Console.WriteLine(sw.Elapsed.TotalNanoseconds.ToString().PadLeft(8));
            sw.Restart();

            // Update all ground station positions based on Earth's rotation
            foreach (var groundStation in groundStations)
            {
                await groundStation.UpdatePosition(simTime);
            }

            Console.WriteLine(sw.Elapsed.TotalNanoseconds.ToString().PadLeft(8));

            // Find and display the nearest satellite for each ground station
            foreach (var groundStation in groundStations)
            {
                Satellite nearestSatellite = groundStation.FindNearestSatellite(satellites);
                Console.WriteLine($"Ground Station {groundStation.Name}: Nearest Satellite = {nearestSatellite.Name} {groundStation.DistanceTo(nearestSatellite)}m \t ({groundStation.Position.X}, {groundStation.Position.Y}, {groundStation.Position.Z})");
            }

            sw.Restart();
            int sum = 0;
            // Find and display the nearest 3 satellites for each satellite
            foreach (var satellite in satellites)
            //Parallel.ForEach(Satellites, satellite =>
            {
                var nearestSatellites = satellites
                    .Where(sat => sat != satellite) // Exclude the current satellite itself
                    .OrderBy(satellite.DistanceTo)
                    .Take(3); // Nearest 3 satellites

                sum += nearestSatellites.Count();

                //Console.WriteLine($"Satellite {satellite.Name}: Nearest Neighbors = {string.Join(", ", nearestSatellites.Select(s => s.Name))} \t ({satellite.Position.X}, {satellite.Position.Y}, {satellite.Position.Z})");
            }
            //);
            Console.WriteLine(sw.Elapsed.TotalNanoseconds.ToString().PadLeft(8));
            Console.WriteLine(sum);

            Thread.Sleep((int)simulationConfiguration.StepInterval * 1000); // 1-second updates
            Console.Clear(); // Clear console for real-time simulation output
        }
    }
}
