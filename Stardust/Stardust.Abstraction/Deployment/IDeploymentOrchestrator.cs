using System.Threading.Tasks;

namespace Stardust.Abstraction.Deployment;

public interface IDeploymentOrchestrator
{
    public string[] DeploymentTypes { get; }

    public Task CreateDeploymentAsync(IDeploymentSpecification deployment);
    public Task DeleteDeploymentAsync(IDeploymentSpecification deployment);
}
