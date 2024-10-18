using StardustLibrary.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public class IslOtherMstProtocol : IInterSatelliteLinkProtocol
{
    private List<IslLink> links = new List<IslLink>(2 ^ 22);
    public ICollection<IslLink> Links { get => links; }

    private List<IslLink> established = [];
    public ICollection<IslLink> Established { get => established; }

    private Satellite? satellite;
    private List<Satellite>? satellites;
    private Dictionary<Satellite, Satellite>? representatives;
    private (double X, double Y, double Z) calculatedPosition;

    private readonly Dictionary<Satellite, IEnumerable<(double Distance, IslLink Link)>> inDistance = new Dictionary<Satellite, IEnumerable<(double Distance, IslLink Link)>>();
    private readonly PriorityQueue<IslLink, double> priorityQueue = new();

    public Task Connect(Satellite satellite)
    {
        throw new System.NotImplementedException();
    }

    public Task Connect(IslLink link)
    {
        established.Add(link);
        return Task.CompletedTask;
    }

    public Task Disconnect(Satellite satellite)
    {
        throw new System.NotImplementedException();
    }

    public Task Disconnect(IslLink link)
    {
        established.Remove(link);
        return Task.CompletedTask;
    }

    public void Mount(Satellite satellite)
    {
        this.satellite ??= satellite;
    }

    public Task<List<IslLink>> UpdateLinks()
    {
        if (satellite == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        if (calculatedPosition == satellite.Position)
        {
            return Task.FromResult(established);
        }
        lock (this)
        {
            if (calculatedPosition == satellite.Position)
            {
                return Task.FromResult(established);
            }
            calculatedPosition = satellite.Position;
        }

        if (satellites == null || representatives == null)
        {
            satellites = satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite1).Concat(satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite2)).Distinct().ToList();
            representatives = satellites.ToDictionary(s => s, s => s);
        }

        var list = new List<IslLink>(satellites.Count);
        inDistance.Clear();
        priorityQueue.Clear();

        var linkDistance = satellite.InterSatelliteLinkProtocol.Links.Select(l => (l.Distance, l)).Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE);
        inDistance.Add(satellite, linkDistance);
        foreach ((double distance, IslLink l) in linkDistance)
        {
            priorityQueue.Enqueue(l, distance);
        }

        while (priorityQueue.Count > 0)
        {
            var link = priorityQueue.Dequeue();
            if (inDistance.ContainsKey(link.Satellite1) && inDistance.ContainsKey(link.Satellite2))
            {
                continue;
            }

            Satellite s = inDistance.ContainsKey(link.Satellite1) ? link.Satellite2 : link.Satellite1;

            linkDistance = s.InterSatelliteLinkProtocol.Links.Select(l => (l.Distance, l)).Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE);
            list.Add(link);
            inDistance.Add(s, linkDistance);
            foreach ((double distance, IslLink l) in linkDistance)
            {
                Satellite other = l.GetOther(s);
                if (!inDistance.ContainsKey(other))
                {
                    priorityQueue.Enqueue(l, distance);
                }
            }
        }

        foreach (var link in list)
        {
            if (!established.Remove(link))
            {
                link.Established = true;
            }
        }

        foreach (var link in established)
        {
            link.Established = false;
        }

        established = list;
        return Task.FromResult(list);
    }

    public void AddLink(IslLink link)
    {
        lock(this)
        {
            links.Add(link);
        }
    }
}
