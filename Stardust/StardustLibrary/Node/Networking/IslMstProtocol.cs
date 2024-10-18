using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public class IslMstProtocol : IInterSatelliteLinkProtocol
{
    private List<IslLink> links = new List<IslLink>(2^20);
    public ICollection<IslLink> Links { get => links; }

    private List<IslLink> established = [];
    public ICollection<IslLink> Established { get => established; }

    private Satellite? satellite;
    private List<Satellite>? satellites;
    private Dictionary<Satellite, Satellite>? representatives;
    private (double X, double Y, double Z) calculatedPosition;

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
            return Task.FromResult(this.established);
        }
        lock (this)
        {
            if (calculatedPosition == satellite.Position)
            {
                return Task.FromResult(this.established);
            }
            calculatedPosition = satellite.Position;
        }

        if (satellites == null || this.representatives == null)
        {
            satellites = satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite1).Concat(satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite2)).Distinct().ToList();
            this.representatives = satellites.ToDictionary(s => s, s => s);
        }

        var list = new List<IslLink>(satellites.Count);
        var representatives = this.representatives.ToDictionary();
        var priorityQueue = new PriorityQueue<IslLink, double>(satellite.InterSatelliteLinkProtocol.Links.Count);

        var links = satellite.InterSatelliteLinkProtocol.Links.Select(l => (l.Distance, l)); //.Where(i => i.Distance <= Physics.MAX_ISL_DISTANCE);
        foreach (var (Distance, l) in links)
        {
            priorityQueue.Enqueue(l, Distance);
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            var rep1 = GetRepresentative(representatives, current.Satellite1);
            var rep2 = GetRepresentative(representatives, current.Satellite2);
            if (rep1 == rep2)
            {
                continue;
            }

            list.Add(current);
            representatives[current.Satellite2] = rep1;
        }

        foreach (var link in list)
        {
            if (!this.established.Remove(link))
            {
                link.Established = true;
            }
        }

        foreach (var link in established)
        {
            link.Established = false;
        }

        this.established = list;

        return Task.FromResult(list);
    }

    private Satellite GetRepresentative(Dictionary<Satellite, Satellite> dict, Satellite satellite)
    {
        Satellite current = satellite;
        while (dict.TryGetValue(current, out var s))
        {
            if (s == current)
            {
                return s;
            }
            current = s;
        }
        return satellite;
    }

    public void AddLink(IslLink link)
    {
        links.Add(link);
    }
}
