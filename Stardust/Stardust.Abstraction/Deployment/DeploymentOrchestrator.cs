using Stardust.Abstraction.Simulation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Deployment;

public class DeploymentOrchestrator : IDeploymentOrchestrator
{
    private readonly DeploymentOrchestratorResolver resolver;
    private readonly List<IDeploymentSpecification> specifications = [];

    public DeploymentOrchestrator(DeploymentOrchestratorResolver resolver, ISimulationController simulationController)
    {
        this.resolver = resolver;
        simulationController.Inject(this);
    }

    public string[] DeploymentTypes => throw new System.NotImplementedException();

    public Task CheckRescheduleAsync(IDeploymentSpecification deployment)
    {
        Parallel.ForEach(specifications, async (spec) =>
        {
            var orchestrator = resolver.Resolve(spec);
            await orchestrator.CheckRescheduleAsync(spec);
        });
        return Task.CompletedTask;
    }

    public Task CreateDeploymentAsync(IDeploymentSpecification deployment)
    {
        specifications.Add(deployment);
        var orchestrator = resolver.Resolve(deployment);
        return orchestrator.CreateDeploymentAsync(deployment);
    }

    public Task DeleteDeploymentAsync(IDeploymentSpecification deployment)
    {
        specifications.Remove(deployment);
        var orchestrator = resolver.Resolve(deployment);
        return orchestrator.DeleteDeploymentAsync(deployment);
    }
}
