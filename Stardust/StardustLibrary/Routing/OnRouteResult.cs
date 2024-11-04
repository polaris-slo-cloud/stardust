using Stardust.Abstraction.Routing;
using System;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class OnRouteResult : IRouteResult
{
    public bool Reachable => true;
    public int Latency { get; }

    private readonly object _lock = new();
    private bool firstRequest = true;
    private int calculationDuration;

    public OnRouteResult(int latency, int calculationDuration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(latency);
        ArgumentOutOfRangeException.ThrowIfNegative(calculationDuration);
        Latency = latency;
        this.calculationDuration = calculationDuration;
    }

    public async Task WaitLatencyAsync()
    {
        int wait = Latency;
        if (firstRequest)
        {
            lock (_lock)
            {
                if (firstRequest)
                {
                    wait -= calculationDuration;
                    firstRequest = false;
                }
            }
        }
        if (wait > 0)
        {
            await Task.Delay(wait);
        }
    }

    public IRouteResult AddCalculationDuration(int calculationDuration)
    {
        lock (_lock)
        {
            if (firstRequest)
            {
                this.calculationDuration += calculationDuration;
            } 
            else
            {
                this.calculationDuration = calculationDuration;
                firstRequest = true;
            }
        }
        return this;
    }
}
