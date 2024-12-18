using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

[Obsolete]
internal class OspfLikeRouter : IRouter
{
    public bool CanPreRouteCalc => true;
    public bool CanOnRouteCalc => false;

    private readonly ConcurrentDictionary<Node, Route> routingTable = new(-1, 2 ^ 12);
    private readonly HashSet<Node> visited = new(2 ^ 12);
    private readonly SortedSet<(ILink, Node, double)> priorityQueue = new(Comparer<(ILink, Node, double)>.Create((l1, l2) => l1.Item1.Latency == l2.Item1.Latency ? 0 : l1.Item1.Latency < l2.Item1.Latency ? -1 : 1));
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
        foreach (var ad in routeAdvertisment.Routes)
        {
            if (routingTable.TryGetValue(ad.Target, out var route))
            {
                if (route.Metric > ad.Metric)
                {
                    routingTable[ad.Target] = new Route(ad.Target, ad.NextHop, ad.Metric);
                }
                continue;
            }
            var learnt = new Route(ad.Target, ad.NextHop, ad.Metric);
            routingTable.TryAdd(ad.Target, learnt);
        }
        return Task.CompletedTask;
    }

    public Task<IRouteResult> RouteAsync(Node target, IPayload? payload)
    {
        if (selfNode == target)
        {
            return Task.FromResult<IRouteResult>(PreRouteResult.ZeroLatencyRoute);
        }

        if (!routingTable.TryGetValue(target, out var route))
        {
            throw new ApplicationException("No route found");
        }
        return route.NextHop.Router.RouteAsync(target, payload);
    }

    public async Task CalculateRoutingTableAsync()
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        routingTable.Clear();
        visited.Clear();
        priorityQueue.Clear();

        visited.Add(selfNode);
        foreach (var link in selfNode.Established)
        {
            priorityQueue.Add((link, selfNode, link.Latency));
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.First();
            priorityQueue.Remove(current);

            var link = current.Item1;
            var advertiser = current.Item2;
            var advertised = link.GetOther(advertiser);
            var latencyToAdvertised = current.Item3;
            if (visited.Contains(advertised))
            {
                continue;
            }

            //await advertised.Router.ReceiveServiceAdvertismentsAsync(new RouteAdvertisment(link, [new Route(selfNode, advertiser, latencyToAdvertised)])).ConfigureAwait(false);
            visited.Add(advertised);
            foreach (var addLink in advertised.Established)
            {
                var other = addLink.GetOther(advertised);
                if (!visited.Contains(other))
                {
                    priorityQueue.Add((addLink, advertised, latencyToAdvertised + addLink.Latency));
                }
            }
        }
    }

    public Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload)
    {
        throw new NotImplementedException();
    }

    public Task AdvertiseNewServiceAsync(string serviceName)
    {
        throw new NotImplementedException();
    }

    public Task ReceiveServiceAdvertismentsAsync(string serviceName, (ILink OutLink, IRouteResult Route) advertised)
    {
        throw new NotImplementedException();
    }
}
