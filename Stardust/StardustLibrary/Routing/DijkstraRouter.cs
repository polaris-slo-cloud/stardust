using Stardust.Abstraction.Computing;
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

    private readonly SortedSet<(ILink, Node, ILink, double)> priorityQueue = new(Comparer<(ILink, Node, ILink, double)>.Create((l1, l2) => l1.Item1.Latency == l2.Item1.Latency ? 0 : l1.Item1.Latency < l2.Item1.Latency ? -1 : 1));
    private readonly Dictionary<Node, (ILink OutLink, double Latency)> routes = [];
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

    public async Task Route(Node target, Workload workload)
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }
        if (target == selfNode)
        {
            return;
        }

        if (!routes.TryGetValue(target, out (ILink OutLink, double Latency) tableEntry) || !selfNode.Established.Contains(tableEntry.OutLink))
        {
            await SendAdvertismentsAsync();
            if (!routes.TryGetValue(target, out tableEntry))
            {
                throw new Exception("No route.");
            }
        }
        await Task.Delay((int)tableEntry.Latency);
    }

    public Task SendAdvertismentsAsync()
    {
        if (selfNode == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        routes.Clear();
        priorityQueue.Clear();

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

            routes.Add(advertised, (advertisedVia, latencyToAdvertised));
            foreach (var addLink in advertised.Established) 
            {
                var other = addLink.GetOther(advertised);
                if (!routes.ContainsKey(other))
                {
                    priorityQueue.Add((addLink, other, advertisedVia, latencyToAdvertised + addLink.Latency));
                }
            }
        }

        return Task.CompletedTask;
    }
}
