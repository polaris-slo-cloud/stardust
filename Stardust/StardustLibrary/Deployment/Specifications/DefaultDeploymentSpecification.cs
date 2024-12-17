using Stardust.Abstraction.Deployment;

namespace StardustLibrary.Deployment.Specifications;

public class DefaultDeploymentSpecification(DeployableService service, ushort replicas) : IDeploymentSpecification
{
    public const string TYPE = "default";
    public string Type => TYPE;
    public DeployableService Service { get; } = service;
    public ushort Replicas { get; } = replicas;
}
