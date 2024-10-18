using System.Collections.Generic;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;
public interface IInterSatelliteLinkProtocol
{
    public ICollection<IslLink> Links { get; }
    public ICollection<IslLink> Established {  get; }

    public Task<List<IslLink>> UpdateLinks();
    public Task Connect(Satellite satellite);
    public Task Connect(IslLink link);
    public Task Disconnect(Satellite satellite);
    public Task Disconnect(IslLink link);
    public void Mount(Satellite satellite);
    public void AddLink(IslLink link);
}
