using System;
using System.Collections.Generic;

namespace Stardust.Abstraction.Deployment;

public class DeploymentOrchestratorResolver
{
    private readonly Dictionary<string, IDeploymentOrchestrator> orchestrators = [];

    public DeploymentOrchestratorResolver(IEnumerable<IDeploymentOrchestrator> orchestrators)
    {
        foreach (var orchestrator in orchestrators)
        {
            foreach (var type in orchestrator.DeploymentTypes)
            {
                if (!this.orchestrators.TryAdd(type, orchestrator))
                {
                    throw new ArgumentException($"Type {type} is duplicated.", nameof(orchestrators));
                }
            }
        }
    }

    public IDeploymentOrchestrator Resolve(IDeploymentSpecification specification)
    {
        if (orchestrators.TryGetValue(specification.Type, out var orchestrator))
        {
            return orchestrator;
        }

        throw new KeyNotFoundException(specification.Type);
    }
}
