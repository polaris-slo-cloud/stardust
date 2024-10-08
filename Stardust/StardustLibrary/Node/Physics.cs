using System;

namespace StardustLibrary.Node;

public static class Physics
{
    /// <summary>
    /// Earth's gravitational parameter in m^3/s^2
    /// </summary>
    public const double MU = 398_600_441_800_000;

    /// <summary>
    /// Earth's radius in meters
    /// </summary>
    public const double EARTH_RADIUS = 6_371_000;

    /// <summary>
    /// Earth's rotation speed in radians per second (2π / 86400 radians/second)
    /// </summary>
    public const double EARTH_ROTATION_SPEED = 2 * Math.PI / 86_400;
}
