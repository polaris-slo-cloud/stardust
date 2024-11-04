using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Deployment;

public class DefaultDeploymentOrchestrator(ISimulationController simulationController) : IDeploymentOrchestrator
{
    private static readonly Random random = new();

    private readonly ISimulationController simulationController = simulationController;
    private readonly Dictionary<DeploymentSpecification, IEnumerable<Node>> deployments = [];

    private List<Node>? nodes = null;
    public List<Node> Nodes {
        get
        {
            nodes ??= simulationController.GetAllNodesAsync().Result;
            return nodes;
        }
    }

    public async Task CreateDeploymentAsync(DeploymentSpecification deployment)
    {
        Shuffle();
        var nodes = Nodes.Where(n => ((int)n.Computing.Type & (int)deployment.Type) != 0 && n.Computing.CpuAvailable >= deployment.Service.Cpu && n.Computing.MemoryAvailable >= deployment.Service.Memory).Take(deployment.Replicas);
        deployments.Add(deployment, nodes);
        foreach (var node in nodes)
        {
            await node.Computing.PlaceDeploymentAsync(deployment.Service);
        }
    }

    public async Task DeleteDeploymentAsync(DeploymentSpecification deployment)
    {
        if (!deployments.TryGetValue(deployment, out var nodes))
        {
            throw new ApplicationException("There is no such deployment to delete");
        }

        foreach (var node in nodes)
        {
            await node.Computing.RemoveDeploymentAsync(deployment.Service);
        }
    }

    private void Shuffle()
    {
        int n = Nodes.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (Nodes[n], Nodes[k]) = (Nodes[k], Nodes[n]);
        }
    }
}
