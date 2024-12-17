using System.Threading.Tasks;

namespace Stardust.Abstraction.Deployment;

public class DeploymentOrchestrator(DeploymentOrchestratorResolver resolver) : IDeploymentOrchestrator
{
    private readonly DeploymentOrchestratorResolver resolver = resolver;

    public string[] DeploymentTypes => throw new System.NotImplementedException();

    public Task CreateDeploymentAsync(IDeploymentSpecification deployment)
    {
        var orchestrator = resolver.Resolve(deployment);
        return orchestrator.CreateDeploymentAsync(deployment);
    }

    public Task DeleteDeploymentAsync(IDeploymentSpecification deployment)
    {
        var orchestrator = resolver.Resolve(deployment);
        return orchestrator.DeleteDeploymentAsync(deployment);
    }
}
