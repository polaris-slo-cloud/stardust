using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class DijkstraRouter : IRouter
{
    public bool CanPreRouteCalc => true;
    public bool CanOnRouteCalc => true;

    private static readonly Comparer<(ILink, Node, ILink, double)> comparer = Comparer<(ILink, Node, ILink, double)>.Create(static (l1, l2) => l1.Item4 == l2.Item4 ? 0 : l1.Item4 < l2.Item4 ? -1 : 1);
    private Dictionary<Node, (ILink OutLink, IRouteResult Route)> routes = [];
    private Dictionary<string, (ILink OutLink, IRouteResult Route)> serviceRoutes = [];
    private Node? selfNode;

    public void Mount(Node node)
    {
        if (selfNode != null)
        {
            throw new MountException("This router is already mounted to a node.");
        }
        selfNode = node;
    }

    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment)
    {
        throw new NotImplementedException();
    }

    public async Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload = null)
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }
        if (selfNode.Computing.Services.Any(s => s.ServiceName == targetServiceName))
        {
            return PreRouteResult.ZeroLatencyRoute;
        }

        if (!serviceRoutes.TryGetValue(targetServiceName, out (ILink OutLink, IRouteResult Result) tableEntry))
        {
            return UnreachableRouteResult.Instance;
        }

        return tableEntry.Result;
    }

    public async Task<IRouteResult> RouteAsync(Node target, IPayload? payload)
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }
        if (target == selfNode)
        {
            return PreRouteResult.ZeroLatencyRoute;
        }

        if (!routes.TryGetValue(target, out (ILink OutLink, IRouteResult Result) tableEntry))
        {
            return UnreachableRouteResult.Instance;
        }

        return tableEntry.Result;
    }

    public Task SendAdvertismentsAsync()
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a node.");
        }

        var routes = new Dictionary<Node, (ILink OutLink, IRouteResult Route)>();
        var serviceRoutes = new Dictionary<string, (ILink OutLink, IRouteResult Route)>();
        var priorityQueue = new SortedSet<(ILink, Node, ILink, double)>(comparer);

        routes.Add(selfNode, default);
        foreach (var link in selfNode.Established)
        {
            priorityQueue.Add((link, link.GetOther(selfNode), link, link.Latency));
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.First();
            priorityQueue.Remove(current);

            var link = current.Item1;
            var advertised = current.Item2;
            var advertisedVia = current.Item3;
            var latencyToAdvertised = current.Item4;
            if (routes.ContainsKey(advertised))
            {
                continue;
            }

            routes.Add(advertised, (advertisedVia, new PreRouteResult((int)latencyToAdvertised)));
            foreach (var service in advertised.Computing.Services)
            {
                if (serviceRoutes.ContainsKey(service.ServiceName))
                {
                    continue;
                }
                serviceRoutes.Add(service.ServiceName, (advertisedVia, new PreRouteResult((int)latencyToAdvertised)));
            }

            foreach (var addLink in advertised.Established)
            {
                var other = addLink.GetOther(advertised);
                if (!routes.ContainsKey(other))
                {
                    priorityQueue.Add((addLink, other, advertisedVia, latencyToAdvertised + addLink.Latency));
                }
            }
        }

        this.routes = routes;
        this.serviceRoutes = serviceRoutes;

        return Task.CompletedTask;
    }
}
