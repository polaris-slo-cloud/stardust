using Stardust.Abstraction.Computing;

namespace Stardust.Abstraction.Deployment;

public interface IDeploymentSpecification
{
    public string Type { get; }
    public DeployableService Service { get; }
}
