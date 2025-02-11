using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Hosting;
using Stardust.Abstraction.Simulation;
using System.Collections.Generic;
using Stardust.Abstraction.Routing;
using System.Linq;
using Stardust.Abstraction.Node;
namespace Stardust;

internal class StatService(ISimulationController simulationController, ILogger<StatService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        var nodes = await simulationController.GetAllNodesAsync();
        var groundNodes = await simulationController.GetAllNodesAsync<GroundStation>();

        logger.LogInformation("{0}", groundNodes.Count);

        var routes = new List<IRouteResult>();

        DateTime start = DateTime.Now;
        await simulationController.StepAsync(0);

        var duration = DateTime.Now - start;
        logger.LogInformation("Done with step after {0}s", duration.TotalSeconds);
        //if (groundNodes.All(n => n.Router.CanPreRouteCalc))
        //{
        //    Parallel.ForEach(groundNodes, async (n) => await n.Router.CalculateRoutingTableAsync());
        //    duration = DateTime.Now - start;
        //    logger.LogInformation("Done with pre route calc after {0}s", duration.TotalSeconds);
        //}
        //Parallel.ForEach(groundNodes, async (source) =>
        //{
        //    var addRoutes = new List<IRouteResult>();
        //    var targets = groundNodes.SkipWhile(n => n != source).Skip(1);
        //    foreach (var target in targets)
        //    {
        //        addRoutes.Add(await source.Router.RouteAsync(target));
        //    }
        //    lock (routes)
        //    {
        //        routes.AddRange(addRoutes);
        //    }
        //    logger.LogInformation(source.Name);
        //});

        var vienna = groundNodes.First(n => n.Name == "Vienna");
        if (vienna.Router.CanPreRouteCalc)
        {
            await vienna.Router.CalculateRoutingTableAsync();
        }
        foreach (var node in groundNodes)
        {
            var route = await vienna.Router.RouteAsync(node);
            routes.Add(route);
            string[] interest = ["Graz", "Paris", "London", "Johannesburg", "Bogota", "New York", "Los Angeles", "Singapore", "Sydney"];
            if (interest.Contains(node.Name))
            {
                if (!route.Reachable)
                {
                    logger.LogWarning($"{node.Name} not reachable");
                } else
                {
                    logger.LogInformation($"Route to {node.Name}: {2 * route.Latency}ms");
                }
            }
        }

        duration = DateTime.Now - start;
        logger.LogInformation("Duration {0}:{1}", duration.Minutes, duration.Seconds);
        logger.LogInformation("Number of routes {0}", routes.Count);
        logger.LogInformation("Average of all routes {0}ms", routes.Average(r => r.Latency));
        logger.LogInformation("Median of all routes {0}ms", routes.Median(r => r.Latency));
    }
}
