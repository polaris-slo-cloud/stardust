using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System.Collections.Generic;

namespace StardustLibrary.Routing;

public class RouterBuilder(RouterConfig config)
{
    private const string DIJKSTRA = "dijkstra";
    private const string A_STAR = "a-star";

    public List<Node> Nodes { get; set; } = [];

    public IRouter Build()
    {
        return config.Protocol switch
        {
            DIJKSTRA => new DijkstraRouter(),
            A_STAR => new AStarRouter(Nodes),
            _ => throw new ConfigurationException("Unknown routing protocol")
        };
    }
}
