using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Deployment.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust;

public class PaperTestService(ISimulationController simulationController, DeploymentOrchestrator orchestrator, ILogger<PaperTestService> logger) : BackgroundService
{
    private const int NUM_SPECS = 25_000;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            try
            {
                await simulationController.StepAsync(0);

                Random r = new Random(1);
                List<TaskSpecification> specifications = [];
                HashSet<Node> routeCalc = [];

                var nodes = await simulationController.GetAllNodesAsync();

                for (int i = 0; i < NUM_SPECS; i++)
                {
                    Node node = nodes[r.Next(nodes.Count)];
                    if (node.Router.CanPreRouteCalc && !routeCalc.Contains(node))
                    {
                        await node.Router.SendAdvertismentsAsync();
                        routeCalc.Add(node);
                    }
                    specifications.Add(new(node, r.Next(30, 100), new DeployableService($"TaskWorkflow{i}", 1, 4)));
                    await orchestrator.CreateDeploymentAsync(specifications[i]);
                }

                var avgCpu = nodes.Select(n => n.Computing).Average(c => c.CpuUsage);
                var avgCpuPercent = nodes.Select(n => n.Computing).Average(c => c.CpuUsagePercent);
                var maxCpuPercent = nodes.Select(n => n.Computing).Max(c => c.CpuUsagePercent);
                var avgMem = nodes.Select(n => n.Computing).Average(c => c.MemoryUsage);
                var avgMemPercent = nodes.Select(n => n.Computing).Average(c => c.MemoryUsagePercent);
                var maxMemPercent = nodes.Select(n => n.Computing).Max(c => c.MemoryUsagePercent);

                logger.LogInformation($"Avg Cpu Usage: {avgCpu} {(avgCpuPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Max Cpu Usage: {(maxCpuPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Avg Mem Usage: {avgMem} {(avgMemPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Max Mem Usage: {(maxMemPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Calculated routing tables: {routeCalc.Count}");

                for (int i = 0; i < NUM_SPECS; i++)
                {
                    await orchestrator.DeleteDeploymentAsync(specifications[i]);
                }

                logger.LogInformation("Finished");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Task Scheduling throws exception");
            }
        }, TaskCreationOptions.LongRunning);
    }
}
