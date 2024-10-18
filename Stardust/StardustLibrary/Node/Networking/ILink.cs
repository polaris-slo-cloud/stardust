namespace StardustLibrary.Node.Networking;

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

    public Node GetOther(Node self);
}
