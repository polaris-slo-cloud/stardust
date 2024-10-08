using System;

namespace StardustLibrary;

internal static class Extensions
{
    public static double DegToRad(this double deg)
    {
        return deg * Math.PI / 180;
    }
}
