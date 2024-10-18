using StardustLibrary.Node.Networking;
using StardustLibrary.Node.Networking.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StardustLibrary.Node;

public class Satellite : Node
{
    public string Name { get; private set; }
    public double Inclination { get; private set; }  // degrees
    public double InclinationRad { get; } // radians
    public double RightAscension { get; private set; }  // degrees
    public double RightAscensionRad { get; } // radians
    public double Eccentricity { get; private set; }  // unitless
    public double ArgumentOfPerigee { get; private set; }  // degrees
    public double ArgumentOfPerigeeRad { get; }
    public double MeanAnomaly { get; private set; }  // degrees
    public double MeanMotion { get; private set; }  // revs per day
    public double SemiMajorAxis { get; private set; }  // meters
    private DateTime Epoch { get; set; }  // TLE epoch
    public IInterSatelliteLinkProtocol InterSatelliteLinkProtocol { get; }
    public List<GroundLink> GroundLinks { get; } = [];

    public override List<ILink> Links { get => InterSatelliteLinkProtocol.Links.Cast<ILink>().ToList(); }
    public override List<ILink> Established { get => InterSatelliteLinkProtocol.Established.Cast<ILink>().ToList(); }

    public Satellite(string name, double inclination, double rightAscension, double eccentricity, double argumetOfPerigee, double meanAnomaly, double meanMotion, DateTime epoch, DateTime simulationTime, IInterSatelliteLinkProtocol interSatelliteLinkProtocol, IRouter router) : base(router)
    {
        Name = name;
        Inclination = inclination;
        InclinationRad = inclination.DegToRad();
        RightAscension = rightAscension;
        RightAscensionRad = rightAscension.DegToRad();
        Eccentricity = eccentricity;
        ArgumentOfPerigee = argumetOfPerigee;
        ArgumentOfPerigeeRad = argumetOfPerigee.DegToRad();
        MeanAnomaly = meanAnomaly;
        MeanMotion = meanMotion;
        Epoch = epoch;
        InterSatelliteLinkProtocol = interSatelliteLinkProtocol;
        InterSatelliteLinkProtocol.Mount(this);
        UpdatePosition(simulationTime).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Satellite(string name, double inclination, double rightAscension, double eccentricity, double argumetOfPerigee, double meanAnomaly, double meanMotion, DateTime epoch, IInterSatelliteLinkProtocol interSatelliteLinkProtocol, IRouter router)
        : this(name, inclination, rightAscension, eccentricity, argumetOfPerigee, meanAnomaly, meanMotion, epoch, DateTime.UtcNow, interSatelliteLinkProtocol, router)
    {
    }

    public override async Task UpdatePosition(DateTime simTime)
    {
        await Task.Delay(0);
        // faster without Task.Run even in parallel with all other tasks
        // 1. Calculate the time difference (Δt) in minutes
        double deltaTSeconds = (simTime - Epoch).TotalSeconds;

        // 2. Mean motion in radians per second (convert from rev/day to rad/second)
        double meanMotionRadPerSec = MeanMotion * 2.0 * Math.PI / (24 * 3600);

        // 3. Calculate new mean anomaly at currentTime
        double meanAnomalyCurrent = MeanAnomaly + meanMotionRadPerSec * deltaTSeconds;
        meanAnomalyCurrent = NormalizeAngle(meanAnomalyCurrent); // Ensure it's in [0, 2π] radians

        // 4. Solve Kepler's Equation for Eccentric Anomaly (iterative method)
        double eccentricAnomaly = SolveKeplersEquation(meanAnomalyCurrent);

        // 5. Calculate true anomaly
        double trueAnomaly = ComputeTrueAnomaly(eccentricAnomaly);

        // 6. Compute the satellite's position in the orbital plane (ECI frame)
        double semiMajorAxis = 6_790_000; // Approx. value in m for LEO satellite, adjust as needed
        double distance = semiMajorAxis * (1 - Eccentricity * Math.Cos(eccentricAnomaly)); // Distance from Earth's center

        // Calculate position in orbital plane (x', y', z' - assuming z' = 0 in orbital plane)
        double xPrime = distance * Math.Cos(trueAnomaly);
        double yPrime = distance * Math.Sin(trueAnomaly);
        double zPrime = 0;

        // 7. Apply orbital transformations to get ECI coordinates
        Position = ApplyOrbitalTransformations(xPrime, yPrime, zPrime);
    }

    // Method to normalize angle to the range [0, 2π]
    private double NormalizeAngle(double angleRadians)
    {
        while (angleRadians < 0) angleRadians += 2.0 * Math.PI;
        while (angleRadians > 2.0 * Math.PI) angleRadians -= 2.0 * Math.PI;
        return angleRadians;
    }

    // Solve Kepler's equation: M = E - e*sin(E) (iterative solution)
    private double SolveKeplersEquation(double meanAnomaly, double tolerance = 1e-6)
    {
        double E = meanAnomaly; // Initial guess: E0 = M0
        double deltaE;
        do
        {
            deltaE = (E - Eccentricity * Math.Sin(E) - meanAnomaly) / (1 - Eccentricity * Math.Cos(E));
            E -= deltaE;
        } while (Math.Abs(deltaE) > tolerance);

        return E; // Eccentric anomaly in radians
    }

    // Compute true anomaly from eccentric anomaly
    private double ComputeTrueAnomaly(double eccentricAnomaly)
    {
        double cosE = Math.Cos(eccentricAnomaly);
        double sinE = Math.Sin(eccentricAnomaly);

        double sqrtOneMinusE2 = Math.Sqrt(1 - Eccentricity * Eccentricity);
        double trueAnomaly = Math.Atan2(sqrtOneMinusE2 * sinE, cosE - Eccentricity);

        return trueAnomaly; // True anomaly in radians
    }

    private (double X, double Y, double Z) ApplyOrbitalTransformations(double xPrime, double yPrime, double zPrime)
    {
        // Convert angles to radians
        double iRad = InclinationRad;
        double omegaRad = ArgumentOfPerigeeRad;
        double raanRad = RightAscensionRad;

        // Perform the rotation matrices for inclination, RAAN, and Argument of Perigee
        double cosRAAN = Math.Cos(raanRad);
        double sinRAAN = Math.Sin(raanRad);
        double cosInclination = Math.Cos(iRad);
        double sinInclination = Math.Sin(iRad);
        double cosArgPerigee = Math.Cos(omegaRad);
        double sinArgPerigee = Math.Sin(omegaRad);

        // Apply the rotations to get the ECI coordinates
        double xECI = (cosRAAN * cosArgPerigee - sinRAAN * sinArgPerigee * cosInclination) * xPrime + (-cosRAAN * sinArgPerigee - sinRAAN * cosArgPerigee * cosInclination) * yPrime;
        double yECI = (sinRAAN * cosArgPerigee + cosRAAN * sinArgPerigee * cosInclination) * xPrime + (-sinRAAN * sinArgPerigee + cosRAAN * cosArgPerigee * cosInclination) * yPrime;
        double zECI = sinInclination * sinArgPerigee * xPrime + sinInclination * cosArgPerigee * yPrime;

        return (xECI, yECI, zECI); // Position in ECI coordinates
    }

    public Task ConfigureConstellation(List<Satellite> satellites)
    {
        return Task.Factory.StartNew((o) =>
        {
            var sats = (List<Satellite>)o!;
            foreach (Satellite satellite in satellites)
            {
                if (satellite == this) // || satellite.interSatelliteLinkProtocol.Links.Any(l => l.Satellite1 == this || l.Satellite2 == this))
                {
                    continue;
                }

                var link = new IslLink(this, satellite);
                this.InterSatelliteLinkProtocol.AddLink(link);
                if (this.InterSatelliteLinkProtocol != satellite.InterSatelliteLinkProtocol)
                {
                    satellite.InterSatelliteLinkProtocol.AddLink(link);
                }
            }
        }, satellites);
    }

}
