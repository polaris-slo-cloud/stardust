using System.Threading.Tasks;

namespace Stardust.Abstraction.Routing;

public interface IRouter
{
    public bool CanPreRouteCalc { get; }
    public bool CanOnRouteCalc { get; }
    public void Mount(Node.Node node);
    public Task SendAdvertismentsAsync();
    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment);
    public Task<IRouteResult> RouteAsync(Node.Node target, IPayload? payload = null);
    public Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload = null);
}
