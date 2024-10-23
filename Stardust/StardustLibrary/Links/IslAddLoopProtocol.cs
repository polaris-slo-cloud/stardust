using Stardust.Abstraction;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslAddLoopProtocol(IInterSatelliteLinkProtocol protocol, InterSatelliteLinkConfig config) : IInterSatelliteLinkProtocol
{
    public ICollection<IslLink> Links => protocol.Links;
    public ICollection<IslLink> Established => protocol.Established;

    public void AddLink(IslLink link)
    {
        protocol.AddLink(link);
    }

    public Task Connect(Satellite satellite)
    {
        return protocol.Connect(satellite);
    }

    public Task Connect(IslLink link)
    {
        return protocol.Connect(link);
    }

    public Task Disconnect(Satellite satellite)
    {
        return protocol.Disconnect(satellite);
    }

    public Task Disconnect(IslLink link)
    {
        return protocol.Disconnect(link);
    }

    public void Mount(Satellite satellite)
    {
        protocol.Mount(satellite);
    }

    public async Task<List<IslLink>> UpdateLinks()
    {
        var list = await protocol.UpdateLinks();
        var established = list.ToList();
        if (established.Count > 0 && established.Count < config.Neighbours - 1)
        {
            (double Distance, IslLink Link) consider = default;
            lock (Links)
            {
                lock (established)
                {
                    consider = Links.Select(l => (l.Distance, l))
                        .Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE &&
                            ShouldLoop(l.l.Satellite1.Established) &&
                            ShouldLoop(l.l.Satellite2.Established) &&
                            !established.Contains(l.l))
                        .OrderBy(l => l.Distance)
                        .FirstOrDefault();
                }
            }

            if (consider != default)
            {
                var link = consider.Link;

                await link.Satellite1.InterSatelliteLinkProtocol.Connect(link);
                await link.Satellite2.InterSatelliteLinkProtocol.Connect(link);
            }
        }

        return established; // TODO loop links are not in established
    }

    private bool ShouldLoop(List<ILink> links) {
        return links.Count < config.Neighbours;
    }
}
