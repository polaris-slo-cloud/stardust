using Stardust.Abstraction.Links;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Routing;

public interface IRouter
{
    public bool CanPreRouteCalc { get; }
    public bool CanOnRouteCalc { get; }
    public void Mount(Node.Node node);
    public Task CalculateRoutingTableAsync();
    public Task AdvertiseNewServiceAsync(string serviceName);
    public Task ReceiveServiceAdvertismentsAsync(string serviceName, (ILink OutLink, IRouteResult Route) advertised);
    public Task<IRouteResult> RouteAsync(Node.Node target, IPayload? payload = null);
    public Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload = null);
}
