using Stardust.Abstraction.Computing;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Routing;

public interface IRouter
{
    public bool CanPreRouteCalc { get; }
    public bool CanOnRouteCalc { get; }
    public void Mount(Node.Node node);
    public Task SendAdvertismentsAsync();
    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment);
    public Task Route(Node.Node target, Workload workload);
}
