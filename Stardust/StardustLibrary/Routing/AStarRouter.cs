using Stardust.Abstraction;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class AStarRouter(List<Node> nodes) : IRouter
{
    public bool CanPreRouteCalc => false;
    public bool CanOnRouteCalc => true;

    private Node? selfNode;
    private readonly List<Node> nodes = nodes;

    public void Mount(Node node)
    {
        if (selfNode != null) {
            throw new MountException("This router already is mounted to a node.");
        }
        selfNode = node;
    }

    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment)
    {
        throw new NotImplementedException();
    }

    public Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload = null)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

        var target = nodes.Where(n => n.Computing.Services.Any(s => s.ServiceName == targetServiceName)).OrderBy(n => n.DistanceTo(selfNode)).FirstOrDefault();
        if (target == null)
        {
            return Task.FromResult<IRouteResult>(UnreachableRouteResult.Instance);
        }
        return RouteAsync(target, payload);
    }

    public Task<IRouteResult> RouteAsync(Node target, IPayload? payload)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

        var start = DateTime.UtcNow;
        var openSet = new PriorityQueue<(Node Node, double G), double>();
        var closedSet = new HashSet<Node>();

        openSet.Enqueue((selfNode, 0), target.DistanceTo(selfNode));
        while (openSet.Count > 0)
        {
            (Node current, double g) = openSet.Dequeue();
            if (closedSet.Contains(current))
            {
                continue;
            }
            if (current == target)
            {
                return Task.FromResult<IRouteResult>(new OnRouteResult((int)(g * 1000 / Physics.SPEED_OF_LIGHT), (int)(DateTime.UtcNow - start).TotalMilliseconds));
            }

            closedSet.Add(current);
            foreach (var link in current.Established)
            {
                Node other = link.GetOther(current);
                if (closedSet.Contains(other))
                {
                    continue;
                }

                double otherG = g + link.Distance;
                openSet.Enqueue((other, otherG), target.DistanceTo(other));
            }
        }
        return Task.FromResult<IRouteResult>(UnreachableRouteResult.Instance);
    }

    public Task SendAdvertismentsAsync()
    {
        throw new NotImplementedException();
    }

}
