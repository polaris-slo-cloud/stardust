using Stardust.Abstraction;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslSatelliteCentricMstProtocol : IInterSatelliteLinkProtocol
{
    private readonly List<IslLink> links = new(2 ^ 22);
    public ICollection<IslLink> Links
    {
        get
        {
            lock (links)
            {
                return links.ToList();
            }
        }
    }

    private List<IslLink> established = [];
    public ICollection<IslLink> Established { get => established; }

    private Satellite? satellite;
    private List<Satellite>? satellites;
    private Vector calculatedPosition;

    private readonly HashSet<Satellite> visited = [];
    private readonly PriorityQueue<IslLink, double> priorityQueue = new();
    private readonly ManualResetEvent resetEvent = new(true);

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
            resetEvent.WaitOne();
            return Task.FromResult(established);
        }
        lock (this)
        {
            if (calculatedPosition == satellite.Position)
            {
                resetEvent.WaitOne();
                return Task.FromResult(established);
            }
            calculatedPosition = satellite.Position;
            resetEvent.Reset();
        }

        satellites ??= satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite1).Concat(satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite2)).Distinct().ToList();

        var list = new List<IslLink>(satellites.Count);
        visited.Clear();
        priorityQueue.Clear();

        var linkDistance = satellite.InterSatelliteLinkProtocol.Links.Select(l => (l.Distance, l)).Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE);
        visited.Add(satellite);
        foreach ((double distance, IslLink l) in linkDistance)
        {
            priorityQueue.Enqueue(l, distance);
        }

        while (list.Count < satellites.Count - 1 && priorityQueue.Count > 0)
        {
            var link = priorityQueue.Dequeue();
            if (visited.Contains(link.Satellite1) && visited.Contains(link.Satellite2))
            {
                continue;
            }

            Satellite s = visited.Contains(link.Satellite1) ? link.Satellite2 : link.Satellite1;

            List<(double Distance, IslLink l)> linkDistance2;
            lock (s.InterSatelliteLinkProtocol.Links)
            {
                linkDistance2 = s.InterSatelliteLinkProtocol.Links.Select(l => (l.Distance, l)).Where(l => l.Distance <= Physics.MAX_ISL_DISTANCE).ToList();
            }
            list.Add(link);
            visited.Add(s);
            foreach ((double distance, IslLink l) in linkDistance2)
            {
                Satellite other = l.GetOther(s);
                if (!visited.Contains(other))
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
        resetEvent.Set();
        return Task.FromResult(list);
    }

    public void AddLink(IslLink link)
    {
        lock (this)
        {
            links.Add(link);
        }
    }
}
