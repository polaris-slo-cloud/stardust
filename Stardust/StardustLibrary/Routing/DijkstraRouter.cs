using Stardust.Abstraction.Computing;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class DijkstraRouter : IRouter
{
    private Node? selfNode;

    public bool CanPreRouteCalc => true;
    public bool CanOnRouteCalc => false;

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

    public Task Route(Node target, Workload workload)
    {
        throw new NotImplementedException();
    }

    public Task SendAdvertismentsAsync()
    {
        throw new NotImplementedException();
    }
}
