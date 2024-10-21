using System.Collections.Generic;
using Stardust.Abstraction.Links;

namespace Stardust.Abstraction.Routing;

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
