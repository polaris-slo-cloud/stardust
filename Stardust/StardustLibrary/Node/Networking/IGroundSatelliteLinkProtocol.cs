using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public interface IGroundSatelliteLinkProtocol
{
    public GroundLink? Link { get; }

    public Task UpdateLink();
    public void Mount(GroundStation groundStation);
}
