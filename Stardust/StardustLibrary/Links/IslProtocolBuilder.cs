using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using StardustLibrary.Links.SatelliteLink;
using System;

namespace StardustLibrary.Links;

public class IslProtocolBuilder
{
    private const string NEAREST = "nearest";
    private const string MST = "mst";
    private const string MST_LOOP = "mst_loop";
    private const string OTHER_MST = "other_mst";
    private const string OTHER_MST_LOOP = "other_mst_loop";

    public static IslProtocolBuilder Builder { get; } = new IslProtocolBuilder();


    private IslMstProtocol? mstProtocol;
    private IslMstProtocol MstProtocol
    {
        get
        {
            lock (this)
            {
                mstProtocol ??= new IslMstProtocol();
            }
            return mstProtocol;
        }
    }

    private IslOtherMstProtocol? otherMstProtocol;
    private IslOtherMstProtocol OtherMstProtocol
    {
        get
        {
            lock (this)
            {
                otherMstProtocol ??= new IslOtherMstProtocol();
            }
            return otherMstProtocol;
        }
    }

    private InterSatelliteLinkConfig? config;

    private IslProtocolBuilder()
    {
    }

    public IslProtocolBuilder SetConfig(InterSatelliteLinkConfig config)
    {
        this.config = config;
        return this;
    }

    public IInterSatelliteLinkProtocol Build()
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Protocol switch
        {
            MST => new IslFilterProtocol(MstProtocol),
            MST_LOOP => new IslAddLoopProtocol(new IslFilterProtocol(MstProtocol), config),
            OTHER_MST => new IslFilterProtocol(OtherMstProtocol),
            OTHER_MST_LOOP => new IslAddLoopProtocol(new IslFilterProtocol(OtherMstProtocol), config),
            NEAREST => new IslNearestProtocol(config),

            _ => throw new ConfigurationException("Unknown ISL protocol.")
        };
    }
}
