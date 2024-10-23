using Stardust.Abstraction;
using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class AStarRouter : IRouter
{
    public bool CanPreRouteCalc => false;
    public bool CanOnRouteCalc => true;

    private Node? selfNode;

    public void Mount(Node node)
    {
        if (selfNode != null) {
            throw new MountException("This router already is mounted to a node.");
        }
        selfNode = node;
    }

    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment)
    {
        throw new System.NotImplementedException();
    }

    public async Task Route(Node target, Workload workload)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

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
                await Task.Delay((int)(g * 1000 / Physics.SPEED_OF_LIGHT));
                //await target.Computing.ScheduleWorkload(workload);
                return;
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

        throw new System.Exception("No route");
    }

    public Task SendAdvertismentsAsync()
    {
        throw new System.NotImplementedException();
    }
}
