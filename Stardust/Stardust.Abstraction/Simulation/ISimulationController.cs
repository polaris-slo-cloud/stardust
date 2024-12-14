using Stardust.Abstraction.Computing;
using System;
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
    /// <returns>The task which executes the autorun steps</returns>
    public Task StopAutorunAsync();

    /// <summary>
    /// Start autorun
    /// </summary>
    /// <returns>The task which executes the autorun steps</returns>
    public Task StartAutorunAsync();

    /// <summary>
    /// Take a simulation step
    /// </summary>
    /// <param name="newSimTime">The new simulation time</param>
    /// <returns>The task which executes the step</returns>
    public Task StepAsync(DateTime newSimTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Take a simulation step
    /// </summary>
    /// <param name="seconds">Seconds to add to previous datetime</param>
    /// <returns>The task which executes the step</returns>
    public Task StepAsync(double seconds, CancellationToken cancellationToken = default);

    public Task<List<Node.Node>> GetAllNodesAsync();
    public Task<List<Node.Node>> GetAllNodesAsync(ComputingType computingType);
    public Task<List<T>> GetAllNodesAsync<T>() where T : Node.Node;
    public Task<List<T>> GetAllNodesAsync<T>(ComputingType computingType) where T : Node.Node;
}
