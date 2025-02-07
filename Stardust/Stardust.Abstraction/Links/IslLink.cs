using System;
using System.Xml.Linq;
using Stardust.Abstraction.Node;

namespace Stardust.Abstraction.Links;

public class IslLink : ILink
{
    private const double SPEED = Physics.SPEED_OF_LIGHT * 0.99;
    public Satellite Satellite1 { get; }
    public Satellite Satellite2 { get; }

    public double Distance
    {
        get
        {
            return Satellite1.DistanceTo(Satellite2);
        }
    }

    public double Latency
    {
        get
        {
            return Distance / SPEED * 1_000;
        }
    }

    public double Bandwidth { get; } = 200_000_000_000; // 200 Gbps static for laser links

    /// <summary>
    /// True if the ISL is established
    /// </summary>
    public bool Established { get; set; }

    public IslLink(Satellite satellite1, Satellite satellite2)
    {
        Satellite1 = satellite1;
        Satellite2 = satellite2;
    }

    public Satellite GetOther(Satellite self)
    {
        if (self == Satellite1)
        {
            return Satellite2;
        }

        if (self == Satellite2)
        {
            return Satellite1;
        }

        throw new ApplicationException("This satellite is not referenced with this link");
    }

    public Node.Node GetOther(Node.Node self)
    {
        if (self == Satellite1)
        {
            return Satellite2;
        }

        if (self == Satellite2)
        {
            return Satellite1;
        }

        throw new ApplicationException("This node is not referenced with this link");
    }

    public bool IsReachable()
    {
        var v = Satellite2.Position - Satellite1.Position;
        var cross = v.CrossProduct(Satellite1.Position);
        var d = cross.Abs() / v.Abs();
        return d > Physics.EARTH_RADIUS + 10_000;
    }
}
