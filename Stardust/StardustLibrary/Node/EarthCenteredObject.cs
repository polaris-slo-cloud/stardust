using System;
using System.Threading.Tasks;

namespace StardustLibrary.Node;

public abstract class EarthCenteredObject
{
    public Computing.Computing Computing { get; }
    public (double X, double Y, double Z) Position { get; protected set; }

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
    public double DistanceTo(EarthCenteredObject other)
    {
        var (x1, y1, z1) = Position;
        var (x2, y2, z2) = other.Position;

        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
    }
}
