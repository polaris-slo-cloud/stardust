using Stardust.Abstraction.Routing;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class UnreachableRouteResult : IRouteResult
{
    public static readonly UnreachableRouteResult Instance = new();

    public bool Reachable => false;
    public int Latency => 0;    // TODO or something like MAX_TIMEOUT?

    public IRouteResult AddCalculationDuration(int calculationDuration)
    {
        return this;
    }

    public Task WaitLatencyAsync()
    {
        return Task.CompletedTask;
    }
}
