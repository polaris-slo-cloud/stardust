using Stardust.Abstraction.Deployment;
using StardustLibrary.Deployment.Specifications;

namespace Stardust.HttpService;

public class HttpDeploymentSpecs : DefaultDeploymentSpecification
{
    public HttpDeploymentSpecs(DeployableService service) : base(service, 1)
    {
    }
}
