using System.Threading.Tasks;

namespace StardustLibrary.Simulation;

public interface ISimulation
{
    public Task Stop();
    public Task Start();
    public Task Step();

    public Task GetAllNodes<T>();
    public Task GetAllNodes()
    {
        return GetAllNodes<object>();
    }
}
