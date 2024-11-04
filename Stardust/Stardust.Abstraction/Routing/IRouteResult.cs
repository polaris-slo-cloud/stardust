using System.Threading.Tasks;

namespace Stardust.Abstraction.Routing;

public interface IRouteResult
{
    public bool Reachable { get; }
    public int Latency { get; }
    public IRouteResult AddCalculationDuration(int calculationDuration);
    public Task WaitLatencyAsync();
}
