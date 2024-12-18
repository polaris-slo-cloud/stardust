using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Node;

namespace StardustLibrary.Deployment.Specifications;

public class TaskSpecification : IDeploymentSpecification
{
    public const string TYPE = "task";

    public string Type => TYPE;
    public string? ServiceName { get; }
    public Node? Node { get; }
    public double MaxLatency { get; }

    public DeployableService Service { get; }

    public TaskSpecification(Node node, double maxLatency, DeployableService service)
    {
        Node = node;
        MaxLatency = maxLatency;
        Service = service;
    }

    public TaskSpecification(string serviceName, double maxLatency, DeployableService service)
    {
        ServiceName = serviceName;
        MaxLatency = maxLatency;
        Service = service;
    }
}
