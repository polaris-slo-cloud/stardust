namespace StardustLibrary.Node.Networking;

public class InterSatelliteLinkConfig
{
    /// <summary>
    /// Number of links to establish
    /// </summary>
    public required int Neighbours { get; set; }

    /// <summary>
    /// Set protocol to use
    /// </summary>
    public required string Protocol { get; set; }
}
