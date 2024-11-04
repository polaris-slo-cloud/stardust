using System.Threading.Tasks;

namespace Stardust.Abstraction.Deployment;

public interface IDeploymentOrchestrator
{
    public Task CreateDeploymentAsync(DeploymentSpecification deployment);
    public Task DeleteDeploymentAsync(DeploymentSpecification deployment);
}
