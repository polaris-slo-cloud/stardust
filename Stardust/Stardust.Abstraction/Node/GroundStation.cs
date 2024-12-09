using Stardust.Abstraction.Links;
using Stardust.Abstraction.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stardust.Abstraction.Node;

public class GroundStation : Node
{
    private double Latitude { get; }
    private double Longitude { get; }
    public DateTime SimulationStartTime { get; }
    public IGroundSatelliteLinkProtocol GroundSatelliteLinkProtocol { get; }

    public override List<ILink> Links => GroundSatelliteLinkProtocol.Link == null ? [] : [GroundSatelliteLinkProtocol.Link];
    public override List<ILink> Established => Links;

    public GroundStation(string name, double longitude, double latitude, IGroundSatelliteLinkProtocol groundSatelliteLinkProtocol, DateTime simulationStartTime, IRouter router, Computing.Computing computing) : base(name, router, computing)
    {
        Latitude = latitude;
        Longitude = longitude;
        SimulationStartTime = simulationStartTime;
        GroundSatelliteLinkProtocol = groundSatelliteLinkProtocol;

        // Set initial position
        UpdatePosition(0);
        groundSatelliteLinkProtocol.Mount(this);
    }

    public GroundStation(string name, double longitude, double latitude, IGroundSatelliteLinkProtocol groundSatelliteLinkProtocol, IRouter router, Computing.Computing computing)
        : this(name, latitude, longitude, groundSatelliteLinkProtocol, DateTime.MinValue, router, computing)
    {
    }

    public override async Task UpdatePosition(DateTime simulationTime)
    {
        double elapsedSeconds = (simulationTime - SimulationStartTime).TotalSeconds;
        UpdatePosition(elapsedSeconds);
        await GroundSatelliteLinkProtocol.UpdateLink();
    }

    // Convert ground station's latitude and longitude to 3D Earth-centered coordinates
    public void UpdatePosition(double timeElapsed)
    {
        // Longitude changes due to Earth's rotation
        double longitudeRad = Longitude.DegToRad() + timeElapsed * Physics.EARTH_ROTATION_SPEED;
        double latitudeRad = Latitude.DegToRad();

        double x = Physics.EARTH_RADIUS * Math.Cos(latitudeRad) * Math.Cos(longitudeRad);
        double y = Physics.EARTH_RADIUS * Math.Cos(latitudeRad) * Math.Sin(longitudeRad);
        double z = Physics.EARTH_RADIUS * Math.Sin(latitudeRad);

        Position = new Vector(x, y, z);
    }

    // Calculate the distance to a satellite
    public double DistanceTo(Satellite satellite)
    {
        var pos1 = Position;
        var pos2 = satellite.Position;

        return Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2) + Math.Pow(pos2.Z - pos1.Z, 2));
    }

    public Satellite FindNearestSatellite(List<Satellite> satellites)
    {
        if (satellites.Count == 0)
        {
            throw new ArgumentException("List must not be empty", nameof(satellites));
        }

        Satellite nearestSatellite = satellites[0];
        double minDistance = DistanceTo(nearestSatellite);
        for (int i = 1; i < satellites.Count; i++)
        {
            var satellite = satellites[i];
            double distance = DistanceTo(satellite);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestSatellite = satellite;
            }
        }

        return nearestSatellite;
    }
}
