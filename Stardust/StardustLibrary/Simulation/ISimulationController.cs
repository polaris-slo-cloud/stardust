using StardustLibrary.Node;
using StardustLibrary.Node.Computing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public interface ISimulationController
{
    public Task<bool> StopAutorunAsync();
    public Task<bool> StartAutorunAsync();
    public Task<bool> StepAsync();
    public Task<List<Node.Node>> GetAllNodesAsync();
    public Task<List<Node.Node>> GetAllNodesAsync(ComputingType computingType);
    public Task<List<T>> GetAllNodesAsync<T>() where T : Node.Node;
    public Task<List<T>> GetAllNodesAsync<T>(ComputingType computingType) where T : Node.Node;

    internal Task WaitForStepAsync(CancellationToken cancellationToken = default);
}
