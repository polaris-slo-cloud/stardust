namespace Stardust.Abstraction.Links;

public interface ILink
{

    /// <summary>
    /// Distance in m
    /// </summary>
    public double Distance { get; }


    /// <summary>
    /// Latency in ms
    /// </summary>
    public double Latency { get; }

    /// <summary>
    /// Bandwidth in bits per second
    /// </summary>
    public double Bandwidth { get; }

    public bool Established { get; }

    public Node.Node GetOther(Node.Node self);
}
