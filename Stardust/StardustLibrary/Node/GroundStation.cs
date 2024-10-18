using StardustLibrary.Node.Networking;
using StardustLibrary.Node.Networking.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StardustLibrary.Node;

public class GroundStation : Node
{

    public string Name { get; }
    private double Latitude { get; }
    private double Longitude { get; }
    public DateTime SimulationStartTime { get; }
    public IGroundSatelliteLinkProtocol GroundSatelliteLinkProtocol { get; }

    public override List<ILink> Links => [GroundSatelliteLinkProtocol.Link];
    public override List<ILink> Established => [GroundSatelliteLinkProtocol.Link];

    public GroundStation(string name, double longitude, double latitude, IGroundSatelliteLinkProtocol groundSatelliteLinkProtocol, DateTime simulationStartTime, IRouter router) : base(router)
    {
        Name = name;
        Latitude = latitude;
        Longitude = longitude;
        SimulationStartTime = simulationStartTime;
        GroundSatelliteLinkProtocol = groundSatelliteLinkProtocol;

        // Set initial position
        UpdatePosition(0);
        groundSatelliteLinkProtocol.Mount(this);
    }

    public GroundStation(string name, double longitude, double latitude, IGroundSatelliteLinkProtocol groundSatelliteLinkProtocol, IRouter router)
        : this(name, latitude, longitude, groundSatelliteLinkProtocol, DateTime.UtcNow, router)
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
        double longitudeRad = Longitude.DegToRad() + (timeElapsed * Physics.EARTH_ROTATION_SPEED);
        double latitudeRad = Latitude.DegToRad();

        double x = Physics.EARTH_RADIUS * Math.Cos(latitudeRad) * Math.Cos(longitudeRad);
        double y = Physics.EARTH_RADIUS * Math.Cos(latitudeRad) * Math.Sin(longitudeRad);
        double z = Physics.EARTH_RADIUS * Math.Sin(latitudeRad);

        Position = (x, y, z);
    }

    // Calculate the distance to a satellite
    public double DistanceTo(Satellite satellite)
    {
        var (x1, y1, z1) = Position;
        var (x2, y2, z2) = satellite.Position;

        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
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
