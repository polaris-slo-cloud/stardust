﻿using Stardust.Abstraction;
using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using Stardust.Abstraction.Node;
using Stardust.Abstraction.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Routing;

public class AStarRouter : IRouter
{
    public bool CanPreRouteCalc => false;
    public bool CanOnRouteCalc => true;

    private Node? selfNode;
    private List<Node> nodes = [];
    private List<Node> Nodes { 
        get
        {
            if (nodes.Count == 0 && selfNode != null)
            {
                nodes = GetNeighbourhood((_) => true).ToList();
            }
            return nodes;
        } 
    }

    public void Mount(Node node)
    {
        if (selfNode != null) {
            throw new MountException("This router already is mounted to a node.");
        }
        selfNode = node;
    }

    public Task<IRouteResult> RouteAsync(string targetServiceName, IPayload? payload = null)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

        var target = Nodes.Where(n => n.Computing.HostsService(targetServiceName)).OrderBy(n => n.DistanceTo(selfNode)).FirstOrDefault();
        if (target == null)
        {
            return Task.FromResult<IRouteResult>(UnreachableRouteResult.Instance);
        }
        return RouteAsync(target, payload);
    }

    public Task<IRouteResult> RouteAsync(Node target, IPayload? payload)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

        var openSet = new SortedSet<(double Score, Node Node)>(
            Comparer<(double Score, Node Node)>.Create((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score) : a.Node.DistanceTo(target).CompareTo(b.Node.DistanceTo(target)))
        );
        var gScores = new Dictionary<Node, double>(); // Cost from start to the node
        var fScores = new Dictionary<Node, double>(); // Estimated cost (g + heuristic)

        gScores[selfNode] = 0;
        fScores[selfNode] = selfNode.DistanceTo(target) / Physics.SPEED_OF_LIGHT * 1_000;
        openSet.Add((fScores[selfNode], selfNode));

        while (openSet.Count > 0)
        {
            // Get the node with the lowest fScore
            var (currentScore, currentNode) = openSet.Min;
            openSet.Remove((currentScore, currentNode));

            if (currentNode == target)
            {
                return Task.FromResult<IRouteResult>(new OnRouteResult((int)gScores[target], 0));
            }

            if (currentScore > fScores[currentNode])
            {
                continue;
            }

            foreach (var link in currentNode.Established)
            {
                var neighbor = link.GetOther(currentNode);
                var tentativeGScore = gScores[currentNode] + link.Latency;

                if (tentativeGScore < gScores.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    // This path is better
                    gScores[neighbor] = tentativeGScore;
                    fScores[neighbor] = tentativeGScore + neighbor.DistanceTo(target) / Physics.SPEED_OF_LIGHT * 1_000;

                    //// Update the open set
                    //if (openSet.Contains((fScores[neighbor], neighbor)))
                    //    openSet.Remove((fScores[neighbor], neighbor));
                    openSet.Add((fScores[neighbor], neighbor));
                }
            }
        }

        return Task.FromResult<IRouteResult>(UnreachableRouteResult.Instance);
    }

    public Task CalculateRoutingTableAsync()
    {
        throw new NotImplementedException();
    }

    public Task AdvertiseNewServiceAsync(string serviceName)
    {
        return Task.CompletedTask;
    }

    public Task ReceiveServiceAdvertismentsAsync(string serviceName, (ILink OutLink, IRouteResult Route) advertised)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Node> GetNeighbourhood(Func<Node, bool> action)
    {
        if (selfNode == null)
        {
            throw new MountException("This router is not mounted on a node.");
        }

        var visited = new HashSet<Node>();
        var queue = new Queue<Node>();

        queue.Enqueue(selfNode);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (visited.Contains(node)) continue;
            if (!action(node)) continue;

            yield return node;
            visited.Add(node);
            foreach (var link in node.Established)
            {
                var other = link.GetOther(node);
                if (visited.Contains(other)) continue;

                queue.Enqueue(other);
            }
        }
    }
}
