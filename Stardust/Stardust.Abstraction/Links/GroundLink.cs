using System;
using Stardust.Abstraction.Node;

namespace Stardust.Abstraction.Links;

public class GroundLink : ILink
{
    public GroundStation GroundStation { get; set; }
    public Satellite Satellite { get; set; }

    public double Distance { get => GroundStation.DistanceTo(Satellite); }

    public double Latency { get => Distance / Physics.SPEED_OF_LIGHT * 1_000; }

    public double Bandwidth { get; } = 500_000_000; // 500Mbit static for now

    public bool Established => true;

    public GroundLink(GroundStation groundStation, Satellite satellite)
    {
        GroundStation = groundStation;
        Satellite = satellite;
    }

    public Node.Node GetOther(Node.Node self)
    {
        if (self == Satellite)
        {
            return GroundStation;
        }


        if (self == GroundStation)
        {
            return Satellite;
        }

        throw new ApplicationException("This ground station is not referenced with this link");
    }
}
