using StardustLibrary.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public class Router
{
    private Dictionary<Satellite, double> routingTable = [];
    private Satellite? satellite;

    public void Mount(Satellite satellite)
    {
        this.satellite = satellite;
    }

    public Task CalculateRoutingTableAsync()
    {
        if (satellite == null)
        {
            throw new MountException("Router is not mounted to a satellite.");
        }

        var visited = new HashSet<Satellite>();
        var distances = new Dictionary<Satellite, double>();
        var priorityQueue = new SortedSet<(ILink, Satellite, double)>(Comparer<(ILink, Satellite, double)>.Create((l1, l2) => l1.Item1.Latency == l2.Item1.Latency ? 0 : (l1.Item1.Latency < l2.Item1.Latency ? -1 : 1)));

        visited.Add(satellite);
        distances.Add(satellite, 0);
        foreach (var link in satellite.InterSatelliteLinkProtocol.Links.Where(l => l.Established))
        {
            var other = link.Satellite1 != satellite ? link.Satellite1 : link.Satellite2;
            priorityQueue.Add((link, other, link.Latency));
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.First();
            priorityQueue.Remove(current);

            var link = current.Item1;
            var advertised = current.Item2;
            var latencyToAdvertised = current.Item3;
            if (visited.Contains(advertised))
            {
                continue;
            }

            distances.Add(advertised, latencyToAdvertised);
            visited.Add(advertised);
            foreach (var addLink in advertised.InterSatelliteLinkProtocol.Links.Where(l => l.Established))
            {
                var other = addLink.Satellite1 != advertised ? addLink.Satellite1 : addLink.Satellite2;
                priorityQueue.Add((addLink, other, latencyToAdvertised + addLink.Latency));
            }
        }

        return Task.CompletedTask;
    }
}
