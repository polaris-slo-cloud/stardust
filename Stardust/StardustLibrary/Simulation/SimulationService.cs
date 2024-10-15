using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Exceptions;
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
    private readonly ISimulationController simulationController;
    private readonly SimulationConfiguration simulationConfiguration;
    private readonly ILogger<SimulationService> logger;

    private DateTime startTime;
    private DateTime simTime;

    private List<Satellite> satellites = [];
    private List<GroundStation> groundStations = [];

    public SimulationService(ISimulationController simulationControllerService, SimulationConfiguration simulationConfiguration, ILogger<SimulationService> logger)
    {
        this.simulationController = simulationControllerService;
        this.simulationConfiguration = simulationConfiguration;
        this.logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Staring simulation ...");

        satellites = await simulationController.GetAllNodesAsync<Satellite>();

        groundStations = await simulationController.GetAllNodesAsync<GroundStation>();

        await base.StartAsync(cancellationToken);
    }
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping simulation ...");
        return base.StopAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            double delta = 0;
            startTime = simTime = DateTime.UtcNow;

            Stopwatch sw = Stopwatch.StartNew();
            while (!stoppingToken.IsCancellationRequested)
            {
                await simulationController.WaitForStepAsync(stoppingToken).ConfigureAwait(false);

                double stepLength = 0;
                if (simulationConfiguration.StepLength != null)
                {
                    stepLength = simulationConfiguration.StepLength.Value;
                } else if (simulationConfiguration.StepMultiplier != null)
                {
                    stepLength = (DateTime.UtcNow - startTime).TotalSeconds * simulationConfiguration.StepMultiplier.Value;
                    startTime = DateTime.UtcNow;
                } else
                {
                    throw new ConfigurationException("Either StepLength or StepMultiplier in simulationConfiguration must be configured!");
                }

                simTime = simTime.AddSeconds(stepLength);
                logger.LogInformation("Simulation time is {0}", simTime.ToString());

                // Update all satellite positions
                logger.LogInformation("UpdatePosition start: {0}ms", sw.ElapsedMilliseconds);
                Parallel.ForEach(satellites, async (s) => await s.UpdatePosition(simTime).ConfigureAwait(false));
                //foreach (var satellite in satellites)
                //{
                //    await satellite.UpdatePosition(simTime).ConfigureAwait(false);
                //}
                logger.LogInformation("UpdatePosition after {0}ms", sw.ElapsedMilliseconds);

                Parallel.ForEach(satellites, async (s) => await s.InterSatelliteLinkProtocol.UpdateLinks().ConfigureAwait(false));
                //foreach (var satellite in satellites)
                //{
                //    await satellite.InterSatelliteLinkProtocol.UpdateLinks().ConfigureAwait(false);
                //}
                logger.LogInformation("UpdateLinks after {0}ms", sw.ElapsedMilliseconds);

                Console.Clear(); // Clear console for real-time simulation output
                Console.WriteLine(sw.Elapsed.TotalSeconds.ToString().PadLeft(8));

                // Update all ground station positions based on Earth's rotation
                foreach (var groundStation in groundStations)
                {
                    await groundStation.UpdatePosition(simTime).ConfigureAwait(false);
                }

                logger.LogInformation("UpdateGroundStation after {0}ms", sw.ElapsedMilliseconds);

                Parallel.ForEach(satellites, async (s) => await s.Router.CalculateRoutingTableAsync());
                //foreach (var satellite in satellites)
                //{
                //    await satellite.Router.CalculateRoutingTableAsync();
                //}

                logger.LogInformation("CalculateRoutingTableAsync after {0}ms", sw.ElapsedMilliseconds);

                // Find and display the nearest satellite for each ground station
                foreach (var groundStation in groundStations)
                {
                    Satellite? nearestSatellite = groundStation.GroundSatelliteLinkProtocol.Link?.Satellite;
                    if (nearestSatellite != null)
                    {
                        Console.WriteLine($"Ground Station {groundStation.Name}: Nearest Satellite = {nearestSatellite.Name} {groundStation.DistanceTo(nearestSatellite)}m \t ({groundStation.Position.X}, {groundStation.Position.Y}, {groundStation.Position.Z})");
                    }
                }

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

                if (sw.Elapsed.Seconds < simulationConfiguration.StepInterval)
                {
                    int wait = (int)((simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) + delta);
                    logger.LogInformation("wait {0}ms", wait);
                    if (wait > 0)
                    {
                        await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                    }
                } else
                {
                    await Task.Delay((int)(sw.ElapsedMilliseconds / 3)).ConfigureAwait(false);
                }
                delta = (delta + simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) / 2;
                logger.LogInformation("Round took {0}ms; delta: {1}ms", sw.ElapsedMilliseconds, delta);
                sw.Restart();
            }
        }, TaskCreationOptions.LongRunning);
    }
}
