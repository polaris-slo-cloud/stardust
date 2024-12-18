using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Deployment.Specifications;
using StardustLibrary.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust;

public class PaperTaskTestService(ISimulationController simulationController, SimulationConfiguration configuration, DeploymentOrchestrator orchestrator, ILogger<PaperTaskTestService> logger) : BackgroundService
{
    private const int NUM_SPECS = 25_000/10;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            try
            {
                await simulationController.StepAsync(0);

                var nodes = await simulationController.GetAllNodesAsync();
                List<TaskSpecification> specifications = [];
                HashSet<Node> routeCalc = [];
                Random r = new(1);

                var start = DateTime.Now;
                for (int i = 0; i < NUM_SPECS; i++)
                {
                    Node node = nodes[r.Next(nodes.Count)];
                    if (node.Router.CanPreRouteCalc && !configuration.UsePreRouteCalc && !routeCalc.Contains(node))
                    {
                        await node.Router.CalculateRoutingTableAsync();
                        routeCalc.Add(node);
                    }
                    specifications.Add(new(node, r.Next(30, 100), new DeployableService($"Task{i}", r.Next(1, 16), r.Next(2, 32))));
                    await orchestrator.CreateDeploymentAsync(specifications[i]);
                }

                var duration = (DateTime.Now - start).TotalSeconds;
                var avgCpu = nodes.Select(n => n.Computing).Average(c => c.CpuUsage);
                var avgCpuPercent = nodes.Select(n => n.Computing).Average(c => c.CpuUsagePercent);
                var midCpuPercent = nodes.Median(n => n.Computing.CpuUsagePercent) ?? throw new Exception("No median");
                var maxCpuPercent = nodes.Select(n => n.Computing).Max(c => c.CpuUsagePercent);
                var avgMem = nodes.Select(n => n.Computing).Average(c => c.MemoryUsage);
                var avgMemPercent = nodes.Select(n => n.Computing).Average(c => c.MemoryUsagePercent);
                var midMemPercent = nodes.Median(n => n.Computing.MemoryUsagePercent) ?? throw new Exception("No median");
                var maxMemPercent = nodes.Select(n => n.Computing).Max(c => c.MemoryUsagePercent);

                logger.LogInformation($"Placement of {NUM_SPECS} tasks on {nodes.Count} nodes took {duration}s");
                logger.LogInformation($"Avg Cpu Usage: {avgCpu} {(avgCpuPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Mid Cpu Usage: {(midCpuPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Max Cpu Usage: {(maxCpuPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Avg Mem Usage: {avgMem} {(avgMemPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Mid Mem Usage: {(midMemPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Max Mem Usage: {(maxMemPercent * 100).ToString("F2")}%");
                logger.LogInformation($"Calculated routing tables: {routeCalc.Count}");

                for (int i = 0; i < NUM_SPECS; i++)
                {
                    await orchestrator.DeleteDeploymentAsync(specifications[i]);
                }

                logger.LogInformation("Finished");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Task Scheduling throws exception");
            }
        }, TaskCreationOptions.LongRunning);
    }
}
