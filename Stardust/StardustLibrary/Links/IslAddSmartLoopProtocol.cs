using Stardust.Abstraction;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslAddSmartLoopProtocol(IInterSatelliteLinkProtocol protocol, InterSatelliteLinkConfig config) : IInterSatelliteLinkProtocol
{
    private List<IslLink> established = [];
    private Satellite? satellite;
    private Vector calculatedPosition;

    private readonly ManualResetEvent resetEvent = new(true);

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
        this.satellite ??= satellite;
        protocol.Mount(satellite);
    }

    public async Task<List<IslLink>> UpdateLinks()
    {
        if (satellite == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        if (calculatedPosition == satellite.Position)
        {
            resetEvent.WaitOne();
            return established;
        }
        lock (this)
        {
            if (calculatedPosition == satellite.Position)
            {
                resetEvent.WaitOne();
                return established;
            }
            calculatedPosition = satellite.Position;
            resetEvent.Reset();
        }

        var mstLinks = await protocol.UpdateLinks();
        var satellites = new Dictionary<Satellite, IslLink>();
        var linkCount = new Dictionary<Satellite, int>();

        foreach (var link in mstLinks)
        {
            if (linkCount.TryAdd(link.Satellite1, 1))
            {
                satellites.Add(link.Satellite1, link);
            }
            else
            {
                linkCount[link.Satellite1]++;
                satellites.Remove(link.Satellite1);
            }

            if (linkCount.TryAdd(link.Satellite2, 1))
            {
                satellites.Add(link.Satellite2, link);
            }
            else
            {
                linkCount[link.Satellite2]++;
                satellites.Remove(link.Satellite2);
            }
        }

        var oneLinkSats = satellites.Select(s => s.Key);
        var consider = oneLinkSats
            .Select(s => (s, SearchForOther(s, oneLinkSats)));

        foreach (var preference in consider)
        {
            Satellite s = preference.s;
            foreach (var link in preference.Item2)
            {
                if (linkCount[s] != 1)
                {
                    break;
                }

                Node node = link.GetOther(s);
                if (node is not Satellite other)
                {
                    continue;
                }
                if (linkCount[s] >= config.Neighbours || linkCount[other] >= config.Neighbours)
                {
                    continue;
                }

                linkCount[s]++;
                linkCount[other]++;
                await protocol.Connect((IslLink)link);
            }
        }

        try
        {
            return established = mstLinks;
        }
        finally
        {
            resetEvent.Set();
        }
    }

    private static IEnumerable<ILink> SearchForOther(Satellite self, IEnumerable<Satellite> satellites)
    {
        return satellites
            .Where(s => s != self)
            .Where(s => s.DistanceTo(self) < Physics.MAX_ISL_DISTANCE)
            .Select(s => (s, self.Position.Dot(s.Position)))
            .OrderBy(s => s.Item2)
            .Select(s => self.Links.First(l => l.GetOther(self) == s.s));
    }
}
