using StardustLibrary.Links.SatelliteLink;
using StardustLibrary.Routing;
using System;

namespace StardustLibrary.DataSource.Satellite;

internal class SatelliteBuilder
{
    private string? name;
    private double inclination;
    private double rightAscension;
    private double eccentricity;
    private double argumetOfPerigee;
    private double meanAnomaly;
    private double meanMotion;
    private DateTime epoch;
    private IslProtocolBuilder? islProtocolBuilder;

    public SatelliteBuilder SetName(string name)
    {
        this.name = name;
        return this;
    }

    public SatelliteBuilder SetInclination(double inclination)
    {
        this.inclination = inclination;
        return this;
    }

    public SatelliteBuilder SetRightAscension(double rightAscension)
    {
        this.rightAscension = rightAscension;
        return this;
    }

    public SatelliteBuilder SetEccentricity(double eccentricity)
    {
        this.eccentricity = eccentricity;
        return this;
    }

    public SatelliteBuilder SetArgumetOfPerigee(double argumetOfPerigee)
    {
        this.argumetOfPerigee = argumetOfPerigee;
        return this;
    }

    public SatelliteBuilder SetMeanAnomaly(double meanAnomaly)
    {
        this.meanAnomaly = meanAnomaly;
        return this;
    }

    public SatelliteBuilder SetMeanMotion(double meanMotion)
    {
        this.meanMotion = meanMotion;
        return this;
    }

    public SatelliteBuilder SetEpoch(DateTime epoch)
    {
        this.epoch = epoch;
        return this;
    }

    public SatelliteBuilder ConfigureIsl(Func<IslProtocolBuilder, IslProtocolBuilder> func)
    {
        islProtocolBuilder = IslProtocolBuilder.Builder;
        islProtocolBuilder = func(islProtocolBuilder);
        return this;
    }

    public Stardust.Abstraction.Node.Satellite Build()
    {
        return new Stardust.Abstraction.Node.Satellite(name, inclination, rightAscension, eccentricity, argumetOfPerigee, meanAnomaly, meanMotion, epoch, islProtocolBuilder.Build(), new DijkstraRouter());
    }
}
