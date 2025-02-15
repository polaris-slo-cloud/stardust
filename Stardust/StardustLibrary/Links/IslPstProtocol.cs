using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.Links;

public class IslPstProtocol : IInterSatelliteLinkProtocol
{
    private readonly HashSet<IslLink> setLink = new(2 ^ 22);
    public ICollection<IslLink> Links { get 
        {
            lock (setLink)
            {
                return setLink.ToList();
            }
        } 
    }

    private List<IslLink> established = [];
    public ICollection<IslLink> Established { get => established; }

    private Satellite? satellite;
    private List<Satellite>? satellites;
    private Dictionary<Satellite, Satellite>? representatives;
    private Vector calculatedPosition;

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

        int maxLinks = 4;
        var representatives = this.representatives.ToDictionary();
        var nodes = new Dictionary<Satellite, int>();
        var mstVertices = new HashSet<IslLink>();
        var partitioner = Partitioner.Create(0, satellites.Count);
        Parallel.ForEach(partitioner, (range, state) =>
        {
            List<IslLink>[] links = new List<IslLink>[range.Item2 - range.Item1];
            for (int i = 0; i < links.Length; i++) 
            {
                var satellite = satellites[range.Item1 + i];
                var representative = GetRepresentative(representatives, satellite);
                var minLink = links[i] = satellite.InterSatelliteLinkProtocol.Links
                    .Where(l => 
                        l.IsReachable() && 
                        representative != GetRepresentative(representatives, l.GetOther(satellite)))
                    //.Where(l => nodes.GetValueOrDefault(l.GetOther(satellite)) < maxLinks)
                    //.Where(l => nodes.GetValueOrDefault(satellite) < maxLinks)
                    .OrderBy(l => l.Latency)
                    .ToList();
            }

            lock (mstVertices)
            {
                for (int i = 0; i < links.Length; i++)
                {
                    var list = links[i];
                    var satellite = satellites[range.Item1 + i];
                    foreach (var l in list)
                    {
                        var other = l.GetOther(satellite);
                        var rep1 = GetRepresentative(representatives, satellite);
                        var rep2 = GetRepresentative(representatives, other);
                        var count1 = nodes.GetValueOrDefault(satellite);
                        var count2 = nodes.GetValueOrDefault(other);
                        if (rep1 == rep2
                            || count1 >= maxLinks
                            || count2 >= maxLinks)
                        {
                            continue;
                        }
                        nodes[satellite] = count1 + 1;
                        nodes[other] = count2 + 1;
                        representatives[rep2] = rep1;
                        representatives[other] = rep1;
                        representatives[satellite] = rep1;
                        mstVertices.Add(l);
                        break;
                    }
                }
            }
        });

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

        this.established = mstVertices.ToList();
        resetEvent.Set();

        return Task.FromResult(this.established);
    }

    public void AddLink(IslLink link)
    {
        lock (setLink)
        {
            setLink.Add(link);
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
