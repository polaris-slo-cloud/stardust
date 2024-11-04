using Microsoft.Extensions.Hosting;
using Stardust.Abstraction.Node;
using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using Stardust.Abstraction.Simulation;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Stardust;

internal class ProgramService(ISimulationController simulationController, ILogger<ProgramService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        var nodes = await simulationController.GetAllNodesAsync();
        var groundStations = await simulationController.GetAllNodesAsync<GroundStation>();

        int steps = 0;
        var sw = Stopwatch.StartNew();
        var source = groundStations[0]; // groundStations[random.Next(0, groundStations.Count - 1)];
        var targets = groundStations.Where(g => g != source);
        var dict = groundStations.ToDictionary(g => g, _ => 0D);
        while (!stoppingToken.IsCancellationRequested)
        {
            steps++;
            await simulationController.StepAsync();
            if (source.Router.CanPreRouteCalc)
            {
                await source.Router.SendAdvertismentsAsync(); // pre routing for dijkstra
            }
            foreach (var target in targets)
            {
                sw.Restart();
                var routeResult = await source.Router.RouteAsync(target);
                await routeResult.WaitLatencyAsync();

                var stop = sw.ElapsedMilliseconds;
                dict[target] = dict[target] + stop;
                logger.LogInformation("Route from {0} to {1} took {2}ms avg {3}ms", source.Name, target.Name, sw.ElapsedMilliseconds, dict[target] / steps);
            }
            await Task.Delay(100, stoppingToken);
        }
    }
}
