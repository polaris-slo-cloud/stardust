using Stardust.Abstraction.Deployment;

namespace Stardust.HttpService;

public class HttpDeploymentSpecs(DeployableService service) : IDeploymentSpecification 
{
    public const string TYPE = "http";
    public string Type => TYPE;

    public DeployableService Service { get; } = service;
}
