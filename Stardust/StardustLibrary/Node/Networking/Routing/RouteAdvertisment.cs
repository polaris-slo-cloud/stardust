using System.Collections.Generic;

namespace StardustLibrary.Node.Networking.Routing;

public class RouteAdvertisment
{
    public RouteAdvertisment(ILink link, IEnumerable<Route> routes)
    {
        Link = link;
        Routes = routes;
    }

    public ILink Link { get; }
    public IEnumerable<Route> Routes { get; }
}
