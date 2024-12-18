using Stardust.Abstraction.Deployment;
using System.Collections.Generic;

namespace StardustLibrary.Deployment.Specifications;

public class WorkflowSpecification : IDeploymentSpecification
{
    public const string TYPE = "workflow";
    public string Type => TYPE;

    public DeployableService Service => throw new System.NotImplementedException();

    public string Name { get; }
    public List<TaskSpecification> Tasks { get; }

    public WorkflowSpecification(string name, List<TaskSpecification> tasks)
    {
        Name = name;
        Tasks = tasks;
    }
}
