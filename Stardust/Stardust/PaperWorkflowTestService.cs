using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Deployment.Specifications;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using StardustLibrary.Simulation;
using System.Linq;
namespace Stardust;

public class PaperWorkflowTestService(ISimulationController simulationController, SimulationConfiguration configuration, DeploymentOrchestrator orchestrator, ILogger<PaperWorkflowTestService> logger) : BackgroundService
{
    private const int NUM_SPECS = 5_000;
    private const int MAX_NUM_TASKS = 5;
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            try
            {
                await simulationController.StepAsync(0);

                var nodes = await simulationController.GetAllNodesAsync();
                List<WorkflowSpecification> specifications = [];
                HashSet<Node> routeCalc = [];
                Random r = new(1);

                var start = DateTime.Now;
                Node node = nodes[r.Next(nodes.Count)];
                for (int i = 0; i < NUM_SPECS; i++)
                {
                    if (node.Router.CanPreRouteCalc && !configuration.UsePreRouteCalc && !routeCalc.Contains(node))
                    {
                        await node.Router.CalculateRoutingTableAsync();
                        routeCalc.Add(node);
                    }

                    int numTasks = r.Next(2, MAX_NUM_TASKS);
                    List<TaskSpecification> tasks = [
                        new TaskSpecification(node, r.Next(30, 100), new DeployableService($"WorkflowTask{i.ToString().PadLeft(5, '0')}{0.ToString().PadLeft(5, '0')}", r.Next(1, 16), r.Next(2, 32)))
                    ];
                    for (int j = 1; j < numTasks; j++)
                    {
                        tasks.Add(new($"WorkflowTask{i.ToString().PadLeft(5, '0')}{(j-1).ToString().PadLeft(5, '0')}", r.Next(30, 100), new DeployableService($"WorkflowTask{i.ToString().PadLeft(5, '0')}{j.ToString().PadLeft(5, '0')}", r.Next(1, 16), r.Next(2, 32))));
                    }
                    specifications.Add(new WorkflowSpecification($"Workflow{i}", tasks));
                }

                Parallel.ForEach(specifications, async s =>
                {
                    await orchestrator.CreateDeploymentAsync(s);
                    logger.LogInformation(s.Name);
                });

                var duration = (DateTime.Now - start).TotalSeconds;
                var avgCpu = nodes.Select(n => n.Computing).Average(c => c.CpuUsage);
                var avgCpuPercent = nodes.Select(n => n.Computing).Average(c => c.CpuUsagePercent);
                var midCpuPercent = nodes.Median(n => n.Computing.CpuUsagePercent) ?? throw new Exception("No median");
                var maxCpuPercent = nodes.Select(n => n.Computing).Max(c => c.CpuUsagePercent);
                var avgMem = nodes.Select(n => n.Computing).Average(c => c.MemoryUsage);
                var avgMemPercent = nodes.Select(n => n.Computing).Average(c => c.MemoryUsagePercent);
                var midMemPercent = nodes.Median(n => n.Computing.MemoryUsagePercent) ?? throw new Exception("No median");
                var maxMemPercent = nodes.Select(n => n.Computing).Max(c => c.MemoryUsagePercent);

                logger.LogInformation($"Placement of {NUM_SPECS} workflows of {specifications.Sum(s => s.Tasks.Count)} tasks on {nodes.Count} nodes took {duration}s");
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
