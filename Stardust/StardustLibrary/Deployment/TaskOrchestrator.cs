using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Deployment.Specifications;
using StardustLibrary.Routing;
using System;
using System.Threading.Tasks;

namespace StardustLibrary.Deployment;

public class TaskOrchestrator(ISimulationController simulationController) : IDeploymentOrchestrator
{
    private readonly ISimulationController simulationController = simulationController;
    private readonly Random random = new(1);

    public string[] DeploymentTypes => [TaskSpecification.TYPE];

    public async Task CreateDeploymentAsync(IDeploymentSpecification deployment)
    {
        if (deployment is not TaskSpecification taskSpecification)
        {
            throw new ArgumentException("Deployment must be a task specification", nameof(deployment));
        }

        Node node;
        IRouteResult route = UnreachableRouteResult.Instance;
        var nodes = await simulationController.GetAllNodesAsync();
        do
        {
            node = nodes[random.Next(nodes.Count)];
            if (!node.Computing.CanPlace(taskSpecification.Service))
            {
                continue;
            }

            route = await taskSpecification.Node.Router.RouteAsync(node);
        } while (!route.Reachable || route.Latency > taskSpecification.MaxLatency);

        await node.Computing.PlaceDeploymentAsync(taskSpecification.Service);
    }

    public Task DeleteDeploymentAsync(IDeploymentSpecification deployment)
    {
        return Task.CompletedTask;
    }
}
