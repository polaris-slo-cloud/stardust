using Stardust.Abstraction.Computing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Simulation;

public interface ISimulationController
{
    /// <summary>
    /// Indicates whether the simulation is running in autorun or manually.
    /// </summary>
    public bool Autorun { get; }

    /// <summary>
    /// Stop autorun
    /// </summary>
    /// <returns>true if autorun stopped</returns>
    public Task<bool> StopAutorunAsync();

    /// <summary>
    /// Start autorun
    /// </summary>
    /// <returns>true if autorun started</returns>
    public Task<bool> StartAutorunAsync();

    /// <summary>
    /// Take a simulation step
    /// </summary>
    /// <returns>true if a step was executed</returns>
    public Task<bool> StepAsync();

    /// <summary>
    /// Signal the end of a step
    /// </summary>
    /// <returns>A task, which waits for end of the step execution</returns>
    public Task SignalStepEndAsync();

    public Task<List<Node.Node>> GetAllNodesAsync();
    public Task<List<Node.Node>> GetAllNodesAsync(ComputingType computingType);
    public Task<List<T>> GetAllNodesAsync<T>() where T : Node.Node;
    public Task<List<T>> GetAllNodesAsync<T>(ComputingType computingType) where T : Node.Node;

    /// <summary>
    /// Wait to take a step
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task, which waits for an initiation of a step execution</returns>
    public Task WaitForStepAsync(CancellationToken cancellationToken = default);
}
