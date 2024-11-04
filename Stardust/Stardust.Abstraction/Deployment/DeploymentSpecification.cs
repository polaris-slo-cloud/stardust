using Stardust.Abstraction.Computing;

namespace Stardust.Abstraction.Deployment;

public class DeploymentSpecification(DeployableService service, ushort replicas, ComputingType type = ComputingType.Any)
{
    public DeployableService Service { get; } = service;
    public ushort Replicas { get; } = replicas;
    public ComputingType Type { get; } = type;
}
