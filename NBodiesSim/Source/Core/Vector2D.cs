/*
 * This is the Vector2D data structure, a two-dimensional vector with double precision. It includes operator overloads
 * as well as basic vector operations, such as obtaining its magnitude, the distance between two vectors, etc.
 */
using System.Numerics;

namespace NBodiesSim.Source.Core;

public struct Vector2D
{
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static Vector2D Zero => new Vector2D(0, 0); // Defines a Vector2D with components (0,0)

    public double X { get; set; }

    public double Y { get; set; }

    // Operator overloads
    
    public static Vector2D operator +(Vector2D operand) => operand;

    public static Vector2D operator -(Vector2D operand) => new Vector2D(-operand.X, -operand.Y);

    public static Vector2D operator +(Vector2D left, Vector2D right) => new Vector2D(left.X + right.X, left.Y + right.Y);

    public static Vector2D operator -(Vector2D left, Vector2D right) => new Vector2D(left.X - right.X, left.Y - right.Y);

    public static Vector2D operator /(Vector2D vector, double escalar)
    {
        return escalar == 0 ? throw new ArgumentException("Cannot divide by 0", nameof(escalar)) :
            new Vector2D(vector.X / escalar, vector.Y / escalar);
    }

    public static Vector2D operator *(Vector2D vector, int escalar) =>
        new Vector2D(vector.X * (double)escalar, vector.Y * (double)escalar);

    public static Vector2D operator *(int escalar, Vector2D vector) =>
        new Vector2D(vector.X * (double)escalar, vector.Y * (double)escalar);

    public static Vector2D operator *(Vector2D vector, double escalar) =>
        new Vector2D(vector.X * escalar, vector.Y * escalar);

    public static Vector2D operator *(double escalar, Vector2D vector) =>
        new Vector2D(vector.X * escalar, vector.Y * escalar);

    // Dot product of two Vector2D vectors
    public static double Dot(Vector2D left, Vector2D right) => (left.X * right.X) + (left.Y * right.Y);
    
    // Returns a Vector2D with components (1,1)

    public static Vector2D One => new Vector2D(1, 1);

    // Unit vector on the X axis
    public static Vector2D UnitX => new Vector2D(1, 0);
    
    // Unit vector on the Y axis
    public static Vector2D UnitY => new Vector2D(0, 1);

    // Normalization of a Vector2D vector
    public static Vector2D Normalize(Vector2D vector)
    {
        double len = vector.Length();
        return len < 1e-10 ? Zero : new Vector2D(vector.X / len, vector.Y / len);
    }

    // Distance between two Vector2D vectors
    public static double Distance(Vector2D left, Vector2D right) =>
        Math.Sqrt(((left.X - right.X) * (left.X - right.X)) + ((left.Y - right.Y) * (left.Y - right.Y)));

    // Squared distance between two Vector2D vectors
    public static double DistanceSquared(Vector2D left, Vector2D right) =>
        ((left.X - right.X) * (left.X - right.X)) + ((left.Y - right.Y) * (left.Y - right.Y));


    public readonly double Length() => Math.Sqrt((X * X) + (Y * Y));

    public readonly double LengthSquared() => (X * X) + (Y * Y);

    // Conversion from Vector2D to Vector2 with a scale factor. Intended for use in the simulator
    public Vector2 ToVector2(float scale) => new Vector2((float)X / scale, (float)Y / scale);
}
