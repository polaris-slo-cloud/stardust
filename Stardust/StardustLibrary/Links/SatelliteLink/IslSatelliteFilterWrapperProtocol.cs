using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Links.SatelliteLink;

public class IslSatelliteFilterWrapperProtocol : IInterSatelliteLinkProtocol
{
    private readonly IInterSatelliteLinkProtocol protocol;
    private Satellite? satellite;

    private readonly List<IslLink> links = [];
    public ICollection<IslLink> Links => links;

    private List<IslLink> established = [];
    public ICollection<IslLink> Established => established;

    public IslSatelliteFilterWrapperProtocol(IInterSatelliteLinkProtocol protocol)
    {
        this.protocol = protocol;
    }

    public void AddLink(IslLink link)
    {
        if (link.Satellite1 == satellite || link.Satellite2 == satellite)
        {
            lock (links)
            {
                links.Add(link);
            }
        }
        protocol.AddLink(link);
    }

    public async Task Connect(Satellite satellite)
    {
        var link = links.Find(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        if (link != null)
        {
            await Connect(link);
        }
    }

    public async Task Connect(IslLink link)
    {
        established.Add(link);
        await protocol.Connect(link);
    }

    public async Task Disconnect(Satellite satellite)
    {
        var link = links.Find(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        if (link != null)
        {
            await Disconnect(link);
        }
    }

    public async Task Disconnect(IslLink link)
    {
        established.Remove(link);
        await protocol.Disconnect(link);
    }

    public void Mount(Satellite satellite)
    {
        if (this.satellite != null)
        {
            throw new MountException("This protocol is already mounted to a satellite");
        }

        this.satellite = satellite;
        protocol.Mount(satellite);
    }

    public async Task<List<IslLink>> UpdateLinks()
    {
        if (satellite == null)
        {
            throw new MountException("This protocol is not mounted to a satellite");
        }

        List<IslLink> list = await protocol.UpdateLinks();
        var established = list.Where(l => l != null).Where(l => l.Satellite1 == satellite || l.Satellite2 == satellite).ToList();
        //if (established.Count > 0 && established.Count <= 2)
        //{
        //    var consider = links.Select(l => (l.Distance, l)).Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE && l.l.GetOther(satellite).Established.Count <= 3 && !established.Contains(l.l)).OrderBy(l => l.Distance).FirstOrDefault();
        //    if (consider != default)
        //    {
        //        var link = consider.l;
        //        var other = link.GetOther(satellite);

        //        link.Established = true;
        //        await other.InterSatelliteLinkProtocol.Connect(link);
        //        await protocol.Connect(link);
        //    }
        //}

        //foreach (var link in this.established)
        //{
        //    if (!established.Contains(link))
        //    {
        //        link.Established = false;
        //    }
        //}

        this.established = established;
        return this.established;
    }
}
