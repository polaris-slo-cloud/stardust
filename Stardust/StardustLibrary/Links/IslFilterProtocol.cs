using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslFilterProtocol(IInterSatelliteLinkProtocol protocol) : IInterSatelliteLinkProtocol
{
    private Satellite? satellite;

    private readonly List<IslLink> links = [];
    public ICollection<IslLink> Links => links;

    private List<IslLink> established = [];
    public ICollection<IslLink> Established
    {
        get
        {
            lock (established)
            {
                return established.ToList();
            }
        }
    }

    public void AddLink(IslLink link)
    {
        if (link.Satellite1 == satellite || link.Satellite2 == satellite)
        {
            lock (links)
            {
                if (!links.Contains(link))
                {
                    links.Add(link);
                }
            }
        }
        protocol.AddLink(link);
    }

    public async Task Connect(Satellite satellite)
    {
        if (satellite == this.satellite)
        {
            throw new ArgumentException("The satellite must not be the mounted satellite.");
        }

        var link = links.Find(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        if (link != null)
        {
            await Connect(link);
        }
    }

    public async Task Connect(IslLink link)
    {
        if (link.Satellite1 == satellite || link.Satellite2 == satellite)
        {
            lock (established)
            {
                if (!established.Contains(link))
                {
                    established.Add(link);
                    link.Established = true;
                }
            }
        }
        await protocol.Connect(link);
    }

    public async Task Disconnect(Satellite satellite)
    {
        if (satellite == this.satellite)
        {
            throw new ArgumentException("The satellite must not be the mounted satellite.");
        }

        var link = links.Find(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        if (link != null)
        {
            await Disconnect(link);
        }
    }

    public async Task Disconnect(IslLink link)
    {
        lock (established)
        {
            established.Remove(link);
            link.Established = false;
        }
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

        var list = await protocol.UpdateLinks();
        return this.established = list.Where(l => l != null).Where(l => l.Satellite1 == satellite || l.Satellite2 == satellite).ToList();
    }
}
