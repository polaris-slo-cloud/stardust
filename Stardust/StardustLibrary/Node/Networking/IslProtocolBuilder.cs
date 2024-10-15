using System;

namespace StardustLibrary.Node.Networking;

public class IslProtocolBuilder
{
    private const string NEAREST = "nearest";

    private InterSatelliteLinkConfig config;

    public IslProtocolBuilder SetConfig(InterSatelliteLinkConfig config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        this.config = config;
        return this;
    }

    public IInterSatelliteLinkProtocol Build()
    {
        switch (config.Protocol)
        {
            case NEAREST:
            default:
                return new IslNearestProtocol(config);
        }
    }
}
