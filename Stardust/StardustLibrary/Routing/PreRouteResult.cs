using Stardust.Abstraction.Routing;
using System;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class PreRouteResult : IRouteResult
{
    public static readonly PreRouteResult ZeroLatencyRoute = new(0);

    public bool Reachable => true;
    public int Latency { get; }

    public PreRouteResult(int latency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(latency);
        Latency = latency;
    }


    public async Task WaitLatencyAsync()
    {
        await Task.Delay(Latency);
    }

    public IRouteResult AddCalculationDuration(int calculationDuration)
    {
        return new OnRouteResult(Latency, calculationDuration);
    }
}
