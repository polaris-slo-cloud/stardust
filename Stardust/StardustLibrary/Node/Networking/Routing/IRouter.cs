using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking.Routing;

public interface IRouter
{
    public void Mount(Node node);
    public Task SendAdvertismentsAsync();
    public Task ReceiveAdvertismentsAsync(RouteAdvertisment routeAdvertisment);
}
