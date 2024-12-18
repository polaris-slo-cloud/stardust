using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using Stardust.Abstraction.Simulation;
using StardustLibrary.Deployment.Specifications;
using StardustLibrary.Routing;
using System;
using System.Linq;
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
        var nodes = (await simulationController.GetAllNodesAsync()).Where(n => n.Computing.CanPlace(taskSpecification.Service)).ToList();
        if (nodes.Count == 0)
        {
            throw new Exception("No node to place service");
        }

        do
        {
            node = nodes[random.Next(nodes.Count)];
            if (taskSpecification.Node != null)
            {
                route = await taskSpecification.Node.Router.RouteAsync(node);
            } else if (!string.IsNullOrEmpty(taskSpecification.ServiceName))
            {
                route = await node.Router.RouteAsync(taskSpecification.ServiceName);
            } else
            {
                throw new InvalidOperationException("Either node or service name must not be null");
            }
        } while (!route.Reachable || route.Latency > taskSpecification.MaxLatency || !await node.Computing.TryPlaceDeploymentAsync(taskSpecification.Service));
    }

    public Task DeleteDeploymentAsync(IDeploymentSpecification deployment)
    {
        return Task.CompletedTask;
    }
}
