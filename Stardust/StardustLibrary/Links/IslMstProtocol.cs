using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslMstProtocol : IInterSatelliteLinkProtocol
{
    private readonly HashSet<IslLink> setLink = new();
    private readonly List<IslLink> links = new(2 ^ 20);
    public ICollection<IslLink> Links { get 
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
    private Dictionary<Satellite, Satellite>? representatives;
    private (double X, double Y, double Z) calculatedPosition;

    private readonly ManualResetEvent resetEvent = new(true);

    public Task Connect(Satellite satellite)
    {
        throw new NotImplementedException();
    }

    public Task Connect(IslLink link)
    {
        lock (established)
        {
            if (!established.Contains(link))
            {
                established.Add(link);
            }
        }
        return Task.CompletedTask;
    }

    public Task Disconnect(Satellite satellite)
    {
        throw new NotImplementedException();
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
            lock (this.established)
            {
                return Task.FromResult(this.established);
            }
        }
        lock (this)
        {
            if (calculatedPosition == satellite.Position)
            {
                resetEvent.WaitOne();
                return Task.FromResult(this.established);
            }
            calculatedPosition = satellite.Position;
            resetEvent.Reset();
        }

        if (satellites == null || this.representatives == null)
        {
            satellites = satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite1).Concat(satellite.InterSatelliteLinkProtocol.Links.Select(s => s.Satellite2)).Distinct().ToList();
            this.representatives = satellites.ToDictionary(s => s, s => s);
        }

        var mstVertices = new List<IslLink>(satellites.Count);
        var representatives = this.representatives.ToDictionary();

        var links = Links.Select(l => (l.Distance, l)).Where(i => i.Distance <= Physics.MAX_ISL_DISTANCE).ToList();
        var priorityQueue = new PriorityQueue<IslLink, double>(links.Count);
        foreach (var (Distance, l) in links)
        {
            priorityQueue.Enqueue(l, Distance);
        }

        // mst has a maximum of satellites.Count - 1 vertices
        while (mstVertices.Count < satellites.Count -1 && priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            var rep1 = GetRepresentative(representatives, current.Satellite1);
            var rep2 = GetRepresentative(representatives, current.Satellite2);
            if (rep1 == rep2)
            {
                continue;
            }

            mstVertices.Add(current);

            // set the representatives dict
            representatives[rep2] = rep1;
            representatives[current.Satellite2] = rep1;
        }

        var established = this.established.ToList();
        foreach (var link in mstVertices)
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

        this.established = mstVertices;
        resetEvent.Set();

        return Task.FromResult(mstVertices);
    }

    public void AddLink(IslLink link)
    {
        lock (this.links)
        {
            if (!setLink.Contains(link))
            {
                links.Add(link);
                setLink.Add(link);
            }
        }
    }

    private static Satellite GetRepresentative(Dictionary<Satellite, Satellite> dict, Satellite satellite)
    {
        Satellite current = satellite;
        try
        {
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
        finally
        {
            // flatten the representatives dict
            dict[satellite] = current;
        }
    }
}
