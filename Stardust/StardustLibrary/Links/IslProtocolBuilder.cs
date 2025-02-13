using Stardust.Abstraction.Exceptions;
using Stardust.Abstraction.Links;
using System;

namespace StardustLibrary.Links;

public class IslProtocolBuilder
{
    private const string NEAREST = "nearest";
    private const string MST = "mst";
    private const string PST = "pst";
    private const string MST_LOOP = "mst_loop";
    private const string PST_LOOP = "pst_loop";
    private const string MST_SMART_LOOP = "mst_smart_loop";
    private const string PST_SMART_LOOP = "pst_smart_loop";
    private const string OTHER_MST = "other_mst";
    private const string OTHER_MST_LOOP = "other_mst_loop";
    private const string OTHER_MST_SMART_LOOP = "other_mst_smart_loop";

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

    private IslPstProtocol? pstProtocol;
    private IslPstProtocol PstProtocol
    {
        get
        {
            lock (this)
            {
                pstProtocol ??= new IslPstProtocol();
            }
            return pstProtocol;
        }
    }

    private IslSatelliteCentricMstProtocol? otherMstProtocol;
    private IslSatelliteCentricMstProtocol OtherMstProtocol
    {
        get
        {
            lock (this)
            {
                otherMstProtocol ??= new IslSatelliteCentricMstProtocol();
            }
            return otherMstProtocol;
        }
    }

    private IslAddSmartLoopProtocol? mstAddSmartLoopProtocol;
    private IslAddSmartLoopProtocol MstAddSmartLoopProtocol
    {
        get
        {
            lock (this)
            {
                mstAddSmartLoopProtocol ??= new IslAddSmartLoopProtocol(MstProtocol, config);
            }
            return mstAddSmartLoopProtocol;
        }
    }


    private IslAddSmartLoopProtocol? pstAddSmartLoopProtocol;
    private IslAddSmartLoopProtocol PstAddSmartLoopProtocol
    {
        get
        {
            lock (this)
            {
                mstAddSmartLoopProtocol ??= new IslAddSmartLoopProtocol(PstProtocol, config);
            }
            return mstAddSmartLoopProtocol;
        }
    }

    private IslAddSmartLoopProtocol? otherMstAddSmartLoopProtocol;
    private IslAddSmartLoopProtocol OtherMstAddSmartLoopProtocol
    {
        get
        {
            lock (this)
            {
                otherMstAddSmartLoopProtocol ??= new IslAddSmartLoopProtocol(OtherMstAddSmartLoopProtocol, config);
            }
            return otherMstAddSmartLoopProtocol;
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
            PST => new IslFilterProtocol(PstProtocol),
            MST_LOOP => new IslAddLoopProtocol(new IslFilterProtocol(MstProtocol), config),
            PST_LOOP => new IslAddLoopProtocol(new IslFilterProtocol(PstProtocol), config),
            MST_SMART_LOOP => new IslFilterProtocol(MstAddSmartLoopProtocol),
            PST_SMART_LOOP => new IslFilterProtocol(PstAddSmartLoopProtocol),
            OTHER_MST => new IslFilterProtocol(OtherMstProtocol),
            OTHER_MST_LOOP => new IslAddLoopProtocol(new IslFilterProtocol(OtherMstProtocol), config),
            OTHER_MST_SMART_LOOP => new IslFilterProtocol(OtherMstAddSmartLoopProtocol),
            NEAREST => new IslNearestProtocol(config),

            _ => throw new ConfigurationException("Unknown ISL protocol.")
        };
    }
}
