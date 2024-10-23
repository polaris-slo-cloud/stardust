using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Routing;

namespace StardustLibrary.Routing;

public class RouterBuilder(RouterConfig config)
{
    private const string DIJKSTRA = "dijkstra";
    private const string A_STAR = "a-star";

    public IRouter Build()
    {
        return config.Protocol switch
        {
            DIJKSTRA => new DijkstraRouter(),
            A_STAR => new AStarRouter(),
            _ => throw new ConfigurationException("Unknown routing protocol")
        };
    }
}
