using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;

namespace StardustLibrary.Deployment.Specifications;

public class TaskSpecification(Node node, double maxLatency, DeployableService service) : IDeploymentSpecification
{
    public const string TYPE = "task";
    public string Type => TYPE;

    public Node Node { get; } = node;
    public double MaxLatency { get; } = maxLatency;

    public DeployableService Service { get; } = service;
}
