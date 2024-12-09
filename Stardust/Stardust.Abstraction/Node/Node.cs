using Stardust.Abstraction.Links;
using Stardust.Abstraction.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Node;

public abstract class Node
{
    public Computing.Computing Computing { get; }
    public IRouter Router { get; set; }
    public string Name { get; }
    public Vector Position { get; protected set; }

    public abstract List<ILink> Links { get; }
    public abstract List<ILink> Established { get; }

    protected Node(string name, IRouter router, Computing.Computing computing)
    {
        Name = name;
        Router = router;
        Computing = computing;

        Router.Mount(this);
    }

    /// <summary>
    /// Calculates the position for a given DateTime.
    /// </summary>
    /// <param name="simulationTime">Calculate the position using this given DateTime.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="simulationTime"/> is invalid (e.g. before the time a satellite existed).</exception>
    /// <returns></returns>
    public abstract Task UpdatePosition(DateTime simulationTime);

    /// <summary>
    /// Calculates the distance to other Earth Centered Objects.
    /// </summary>
    /// <param name="other">The other Earth Centered Object</param>
    /// <returns>The distance to the other object.</returns>
    public double DistanceTo(Node other)
    {
        var pos1 = Position;
        var pos2 = other.Position;

        return Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2) + Math.Pow(pos2.Z - pos1.Z, 2));
    }
}
