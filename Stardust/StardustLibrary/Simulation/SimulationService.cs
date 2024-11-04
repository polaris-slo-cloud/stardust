using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public class SimulationService : BackgroundService
{
    private readonly ISimulationController simulationController;
    private readonly SimulationConfiguration simulationConfiguration;
    private readonly ILogger<SimulationService> logger;
    private readonly ParallelOptions _parallelOptions;

    private DateTime startTime;
    private DateTime simTime;

    private List<Satellite> satellites = [];
    private List<GroundStation> groundStations = [];
    private List<Node> nodes = [];

    public SimulationService(ISimulationController simulationControllerService, SimulationConfiguration simulationConfiguration, ILogger<SimulationService> logger)
    {
        this.simulationController = simulationControllerService;
        this.simulationConfiguration = simulationConfiguration;
        this.logger = logger;
        this._parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = simulationConfiguration.MaxCpuCores,
        };
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Staring simulation ...");

        satellites = await simulationController.GetAllNodesAsync<Satellite>();
        groundStations = await simulationController.GetAllNodesAsync<GroundStation>();
        nodes = await simulationController.GetAllNodesAsync();

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
            try
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
                    }
                    else if (simulationConfiguration.StepMultiplier != null)
                    {
                        stepLength = (DateTime.UtcNow - startTime).TotalSeconds * simulationConfiguration.StepMultiplier.Value;
                        startTime = DateTime.UtcNow;
                    }
                    else
                    {
                        throw new ConfigurationException("Either StepLength or StepMultiplier in simulationConfiguration must be configured!");
                    }

                    simTime = simTime.AddSeconds(stepLength);
                    logger.LogInformation("Simulation time is {0}", simTime.ToString());

                    // Update all node positions
                    Parallel.ForEach(nodes, _parallelOptions, async (n) => await n.UpdatePosition(simTime).ConfigureAwait(false));
                    logger.LogInformation("UpdatePosition after {0}ms", sw.ElapsedMilliseconds);

                    Parallel.ForEach(satellites, _parallelOptions, async (s) => await s.InterSatelliteLinkProtocol.UpdateLinks().ConfigureAwait(false));
                    Parallel.ForEach(groundStations, _parallelOptions, async (g) => await g.GroundSatelliteLinkProtocol.UpdateLink().ConfigureAwait(false));
                    logger.LogInformation("UpdateLinks after {0}ms", sw.ElapsedMilliseconds);

                    if (simulationConfiguration.UsePreRouteCalc)
                    {
                        Parallel.ForEach(satellites, _parallelOptions, async (s) => await s.Router.SendAdvertismentsAsync().ConfigureAwait(false));
                        logger.LogInformation("CalculateRoutingTableAsync after {0}ms", sw.ElapsedMilliseconds);
                    }

                    // Find and display the nearest satellite for each ground station
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        foreach (var groundStation in groundStations)
                        {
                            Satellite? nearestSatellite = groundStation.GroundSatelliteLinkProtocol.Link?.Satellite;
                            if (nearestSatellite != null)
                            {
                                logger.LogTrace($"Ground Station {groundStation.Name}: Nearest Satellite = {nearestSatellite.Name} {groundStation.DistanceTo(nearestSatellite)}m \t ({groundStation.Position.X}, {groundStation.Position.Y}, {groundStation.Position.Z})");
                            }
                        }
                    }

                    if (sw.Elapsed.Seconds < simulationConfiguration.StepInterval)
                    {
                        int wait = (int)((simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) + delta);
                        logger.LogInformation("wait {0}ms", wait);
                        if (wait > 0)
                        {
                            await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                        }
                        delta = (delta + simulationConfiguration.StepInterval * 1_000 - sw.ElapsedMilliseconds) / 2;
                    }
                    else
                    {
                        await Task.Delay((int)(sw.ElapsedMilliseconds / 3)).ConfigureAwait(false);
                    }
                    if (simulationConfiguration.StepInterval < 0)
                    {
                        await simulationController.StepEndAsync();
                    }
                    logger.LogInformation("Round took {0}ms; delta: {1}ms", sw.ElapsedMilliseconds, delta);
                    sw.Restart();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in ExecuteAsync");
                throw;
            }
        }, TaskCreationOptions.LongRunning);
    }
}
