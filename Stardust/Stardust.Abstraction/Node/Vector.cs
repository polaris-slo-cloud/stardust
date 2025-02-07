using System;
using System.Diagnostics.CodeAnalysis;

namespace Stardust.Abstraction.Node;

public struct Vector
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Vector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public readonly double Abs()
    {
        return Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public readonly double Dot(Vector v)
    {
        return this.X * v.X + this.Y * v.Y + this.Z * v.Z;
    }

    public readonly Vector CrossProduct(Vector other)
    {
        return new Vector()
        {
            X = this.Y * other.Z - this.Z * other.X,
            Y = this.Z * other.X - this.X * other.Y,
            Z = this.X * other.Y - this.Y * other.Z,
        };
    }

    public readonly Vector Normalize()
    {
        var lenght = Math.Sqrt(X * X + Y * Y + Z * Z);
        return new Vector(X / lenght, Y / lenght, Z / lenght);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null) return false;
        if (obj is not Vector v) return false;

        return v == this;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Vector a, Vector b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vector a, Vector b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

    public static Vector operator -(Vector a, Vector b) => new(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
}
