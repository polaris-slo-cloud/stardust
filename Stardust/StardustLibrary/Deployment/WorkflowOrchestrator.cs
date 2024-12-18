using Stardust.Abstraction.Deployment;
using StardustLibrary.Deployment.Specifications;
using System;
using System.Threading.Tasks;

namespace StardustLibrary.Deployment;

public class WorkflowOrchestrator(TaskOrchestrator taskOrchestrator) : IDeploymentOrchestrator
{
    public string[] DeploymentTypes => [WorkflowSpecification.TYPE];

    public async Task CreateDeploymentAsync(IDeploymentSpecification deployment)
    {
        if (deployment is not WorkflowSpecification workflowSpecification)
        {
            throw new ArgumentException("Deployment must be a task specification", nameof(deployment));
        }

        foreach (var task in workflowSpecification.Tasks)
        {
            await taskOrchestrator.CreateDeploymentAsync(task);
        }
    }

    public async Task DeleteDeploymentAsync(IDeploymentSpecification deployment)
    {
        if (deployment is not WorkflowSpecification workflowSpecification)
        {
            throw new ArgumentException("Deployment must be a task specification", nameof(deployment));
        }

        foreach (var task in workflowSpecification.Tasks)
        {
            await taskOrchestrator.DeleteDeploymentAsync(deployment);
        }
    }
}
