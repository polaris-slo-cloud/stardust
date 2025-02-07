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

    // Calculate 3D Earth-centered coordinates from ground station's latitude and longitude
    public void UpdatePosition(double timeElapsed)
    {
        double alt = 0; // altitude can be added later

        // WGS84 ellipsoid constants
        double a = 6378137.0;          // semi-major axis in meters
        double b = 6356752.314245;     // semi-minor axis in meters
        double e2 = 1 - (b * b) / (a * a); // first eccentricity squared

        // Convert degrees to radians
        double latRad = Latitude.DegToRad();
        double lonRad = Longitude.DegToRad();

        // Calculate prime vertical radius of curvature
        double N = a / Math.Sqrt(1 - e2 * Math.Sin(lonRad) * Math.Sin(lonRad));


        // Calculate ECEF coordinates
        double x = (N + alt) * Math.Cos(lonRad) * Math.Cos(latRad);
        double y = (N + alt) * Math.Cos(lonRad) * Math.Sin(latRad);
        double z = (b * b / (a * a) * N + alt) * Math.Sin(lonRad);

        // Calculate rotation (assume no titled earth axis)
        double theta = Physics.EARTH_ROTATION_SPEED * timeElapsed;
        double xRotated = x * Math.Cos(theta) - y * Math.Sin(theta);
        double yRotated = x * Math.Sin(theta) + y * Math.Cos(theta);
        double zRotated = z;

        Position = new Vector(xRotated, yRotated, zRotated);
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
