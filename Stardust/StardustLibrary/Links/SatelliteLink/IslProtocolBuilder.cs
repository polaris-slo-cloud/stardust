using Stardust.Abstraction.Links;
using System;

namespace StardustLibrary.Links.SatelliteLink;

public class IslProtocolBuilder
{
    private const string NEAREST = "nearest";
    private const string MST = "mst";
    private const string OTHER_MST = "other_mst";

    public static IslProtocolBuilder Builder { get; private set; } = new IslProtocolBuilder();

    private InterSatelliteLinkConfig? config;
    private IslMstProtocol? mstProtocol;
    private IslOtherMstProtocol? otherMstProtocol;

    private IslProtocolBuilder()
    {
    }

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
            case MST:
                if (mstProtocol == null)
                {
                    lock (this)
                    {
                        mstProtocol ??= new IslMstProtocol();
                    }
                }
                return new IslSatelliteFilterWrapperProtocol(mstProtocol);
            case OTHER_MST:
                if (otherMstProtocol == null)
                {
                    lock (this)
                    {
                        otherMstProtocol ??= new IslOtherMstProtocol();
                    }
                }
                return new IslSatelliteFilterWrapperProtocol(otherMstProtocol);
            case NEAREST:
            default:
                return new IslNearestProtocol(config);
        }
    }
}
