using StardustLibrary.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public class IslNearestProtocol : IInterSatelliteLinkProtocol
{
    private readonly InterSatelliteLinkConfig config;
    private Satellite? satellite;

    private List<IslLink> links = [];
    public ICollection<IslLink> Links { get => links; }

    public ICollection<IslLink> Established { get => outgoing.Concat(incoming).Distinct().ToList(); }

    private List<IslLink> outgoing = [];
    private readonly List<IslLink> incoming = [];

    public IslNearestProtocol(InterSatelliteLinkConfig config)
    {
        this.config = config;
    }

    public async Task Connect(Satellite satellite)
    {
        var link = Links.First(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        await Connect(link);
    }

    public Task Connect(IslLink link)
    {
        if (!incoming.Contains(link))
        {
            incoming.Add(link);
        }
        return Task.CompletedTask;
    }

    public async Task Disconnect(Satellite satellite)
    {
        var link = Links.First(l => l.Satellite1 == satellite || l.Satellite2 == satellite);
        await Disconnect(link);
    }

    public Task Disconnect(IslLink link)
    {
        incoming.Remove(link);
        if (!outgoing.Contains(link))
        {
            link.Established = false;
        }
        return Task.CompletedTask;
    }

    public void Mount(Satellite satellite)
    {
        this.satellite = satellite;
    }

    public Task<List<IslLink>> UpdateLinks()
    {
        return Task.Run(() =>
        {
            if (satellite is null)
            {
                throw new MountException("The isl protocol is not mounted to a satellite.");
            }

            var prevOut = outgoing;
            outgoing = Links.OrderBy(l => l.Distance)
                .TakeWhile(l => l.Distance <= Physics.MAX_ISL_DISTANCE)
                //.Take(config.Neighbours / 2)
                .ToList();

            foreach (var link in outgoing)
            {
                prevOut.Remove(link);
                if (link.Established)
                {
                    continue;
                }

                var other = link.Satellite1 == satellite ? link.Satellite2 : link.Satellite1;
                link.Established = true;
                other.InterSatelliteLinkProtocol.Connect(link);
            }

            foreach (var link in prevOut)
            {
                var other = link.Satellite1 == satellite ? link.Satellite2 : link.Satellite1;
                other.InterSatelliteLinkProtocol.Disconnect(link);
            }

            return outgoing;
        });
    }

    public void AddLink(IslLink link)
    {
        links.Add(link);
    }
}
