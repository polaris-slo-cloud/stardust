namespace StardustLibrary.Node.Networking;

public class IslLink : ILink
{
    public Satellite Satellite1 { get; }
    public Satellite Satellite2 { get; }

    public double Distance { get
        {
            return Satellite1.DistanceTo(Satellite2);
        }
    }

    public double Latency { get
        {
            return Distance / Physics.SPEED_OF_LIGHT * 1_000;
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
}
