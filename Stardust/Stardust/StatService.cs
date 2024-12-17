using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Hosting;
using Stardust.Abstraction.Simulation;
using System.Collections.Generic;
using Stardust.Abstraction.Routing;
using System.Linq;
namespace Stardust;

internal class StatService(ISimulationController simulationController, ILogger<StatService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        var nodes = await simulationController.GetAllNodesAsync();

        var routes = new List<IRouteResult>();

        await simulationController.StepAsync(0);

        DateTime start = DateTime.Now;
        logger.LogInformation("Done with step");
        if (nodes.All(n => n.Router.CanPreRouteCalc))
        {
            Parallel.ForEach(nodes, async (n) => await n.Router.SendAdvertismentsAsync());
            logger.LogInformation("Done with pre route calc");
        }

        Parallel.ForEach(nodes, async (source) =>
        {
            var tasks = nodes.SkipWhile(n => n != source).Skip(1).Select(n => source.Router.RouteAsync(n));
            var addRoutes = await Task.WhenAll(tasks);
            lock (routes)
            {
                routes.AddRange(addRoutes);
            }
            logger.LogInformation(source.Name);
        });

        var duration = DateTime.Now - start;
        logger.LogInformation("Duration {0}:{1}", duration.Minutes, duration.Seconds);
        logger.LogInformation("Number of routes {0}", routes.Count);
        logger.LogInformation("Average of all routes {0}ms", routes.Average(r => r.Latency));
        logger.LogInformation("Median of all routes {0}ms", Median(routes).Latency);
    }

    private static IRouteResult Median(IEnumerable<IRouteResult> routes) 
    {
        int mid = routes.Count() / 2;
        routes = routes.OrderBy(r => r.Latency);
        return routes.ElementAt(mid);
    }
}
