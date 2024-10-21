using System.Threading.Tasks;
using Stardust.Abstraction.Node;

namespace Stardust.Abstraction.Links;

public interface IGroundSatelliteLinkProtocol
{
    public GroundLink? Link { get; }

    public Task UpdateLink();
    public void Mount(GroundStation groundStation);
}
