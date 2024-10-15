using StardustLibrary.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node.Networking;

public class GroundLinkNearestProtocol : IGroundSatelliteLinkProtocol
{
    public GroundLink? Link { get; private set; }

    private readonly List<Satellite> satellites;
    private GroundStation? groundStation;

    public GroundLinkNearestProtocol(List<Satellite> satellites)
    {
        this.satellites = satellites;
    }

    public void Mount(GroundStation groundStation)
    {
        if (this.groundStation != null)
        {
            throw new MountException("Protocol already linked to ground station.");
        }
        this.groundStation = groundStation;
    }

    public Task UpdateLink()
    {
        if (groundStation == null)
        {
            throw new MountException("Protocol is not mounted to ground station.");
        }

        var uplinkSat = satellites.OrderBy(groundStation.DistanceTo).FirstOrDefault();
        if (uplinkSat == null || Link?.Satellite == uplinkSat)
        {
            return Task.CompletedTask;
        }

        var old = Link;
        Link = new GroundLink(groundStation, uplinkSat);
        uplinkSat.GroundLinks.Add(Link);
        old?.Satellite.GroundLinks.RemoveAll(l => l.GroundStation == groundStation);

        return Task.CompletedTask;
    }
}
