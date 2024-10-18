using StardustLibrary.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking.Routing;

internal class OspfRouter : IRouter
{
    private readonly Dictionary<Node, Route> routingTable = [];
    private Node? node;

    public OspfRouter()
    {
        
    }

    public void Mount(Node node)
    {
        this.node = node;
    }

    public async Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment)
    {
        var learnt = ProcessAsvertisementsAsync(routeAdvertisment);
        await SendAdvertismentsAsync(learnt);
    }

    public async Task SendAdvertismentsAsync()
    {
        if (node == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        var established = node.Links.Where(l => l.Established);
        var routes = established.Select(l => new Route(l.GetOther(node), node, l.Latency)); // (l, l.Satellite1 != satellite ? l.Satellite1 : l.Satellite2, l.Latency));

        await SendAdvertismentsAsync(routes, established);
    }

    private IEnumerable<Route> ProcessAsvertisementsAsync(RouteAdvertisment routeAdvertisment)
    {
        foreach (var ad in routeAdvertisment.Routes)
        {
            if (!routingTable.TryGetValue(ad.Target, out var route))
            {
                var learnt = new Route(ad.Target, ad.NextHop, ad.Metric + routeAdvertisment.Link.Latency);
                routingTable.Add(ad.Target, learnt);
                yield return learnt;
                continue;
            }
            if (route.Metric > ad.Metric + routeAdvertisment.Link.Latency)
            {
                var learnt = new Route(ad.Target, ad.NextHop, ad.Metric + routeAdvertisment.Link.Latency);
                routingTable[ad.Target] = learnt;
                yield return learnt;
            }
        }
    }

    private async Task SendAdvertismentsAsync(IEnumerable<Route> routes, IEnumerable<ILink>? established = default)
    {
        if (node == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        established = established ?? node.Links.Where(l => l.Established);
        foreach (var link in established)
        {
            var other = link.GetOther(node);
            await other.Router.ReceiveAdvertismentsAsync(new RouteAdvertisment(link, routes));
        }
    }
}
