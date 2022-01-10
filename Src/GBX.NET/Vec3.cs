﻿namespace GBX.NET;

public readonly record struct Vec3(float X, float Y, float Z) : IVec
{
    public Vec3 GetXY() => new(X, Y, 0);
    public Vec3 GetXZ() => new(X, 0, Z);
    public Vec3 GetYZ() => new(0, Y, Z);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public float GetMagnitude() => MathF.Sqrt(GetSqrMagnitude());
#endif

#if NETSTANDARD2_0 || NET462_OR_GREATER
    public float GetMagnitude() => (float)Math.Sqrt(GetSqrMagnitude());
#endif

    public float GetSqrMagnitude() => X * X + Y * Y + Z * Z;

    public static readonly Vec3 Zero = new();
    public static float GetDotProduct(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator +(Vec3 a, Int3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator +(Vec3 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y, a.Z);
    public static Vec3 operator +(Vec3 a, Int2 b) => new(a.X + b.X, a.Y + b.Y, a.Z);
    public static Vec3 operator +(Vec3 a, int b) => new(a.X + b, a.Y + b, a.Z + b);
    public static Vec3 operator +(Vec3 a, float b) => new(a.X + b, a.Y + b, a.Z + b);

    public static Vec3 operator +(Int3 a, Vec3 b) => b + a;
    public static Vec3 operator +(Vec2 a, Vec3 b) => b + a;
    public static Vec3 operator +(Int2 a, Vec3 b) => b + a;
    public static Vec3 operator +(int a, Vec3 b) => b + a;
    public static Vec3 operator +(float a, Vec3 b) => b + a;

    public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator -(Vec3 a, Int3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator -(Vec3 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y, a.Z);
    public static Vec3 operator -(Vec3 a, Int2 b) => new(a.X - b.X, a.Y - b.Y, a.Z);
    public static Vec3 operator -(Vec3 a, int b) => new(a.X - b, a.Y - b, a.Z - b);
    public static Vec3 operator -(Vec3 a, float b) => new(a.X - b, a.Y - b, a.Z - b);

    public static Vec3 operator *(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vec3 operator *(Vec3 a, Int3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vec3 operator *(Vec3 a, Vec2 b) => new(a.X * b.X, a.Y * b.Y, a.Z);
    public static Vec3 operator *(Vec3 a, Int2 b) => new(a.X * b.X, a.Y * b.Y, a.Z);
    public static Vec3 operator *(Vec3 a, int b) => new(a.X * b, a.Y * b, a.Z * b);
    public static Vec3 operator *(Vec3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);

    public static Vec3 operator *(Int3 a, Vec3 b) => b * a;
    public static Vec3 operator *(Vec2 a, Vec3 b) => b * a;
    public static Vec3 operator *(Int2 a, Vec3 b) => b * a;
    public static Vec3 operator *(int a, Vec3 b) => b * a;
    public static Vec3 operator *(float a, Vec3 b) => b * a;

    public static implicit operator Vec3(Int3 a) => new(a.X, a.Y, a.Z);
    public static implicit operator Vec3((float X, float Y, float Z) v) => new(v.X, v.Y, v.Z);
    public static implicit operator (float X, float Y, float Z)(Vec3 v) => (v.X, v.Y, v.Z);

    public static explicit operator Vec3(Byte3 a) => new(a.X, a.Y, a.Z);
    public static explicit operator Vec3(Int2 a) => new(a.X, 0, a.Y);
    public static explicit operator Vec3(Vec2 a) => new(a.X, a.Y, 0);
    public static explicit operator Vec3(Vec4 a) => new(a.X, a.Y, a.Z);

    public static explicit operator Vec3(ReadOnlySpan<float> a) => GetVec3FromReadOnlySpan(a);
    public static explicit operator Vec3(Span<float> a) => GetVec3FromReadOnlySpan(a);
    public static explicit operator Vec3(float[] a) => GetVec3FromReadOnlySpan(a);

    public static Vec3 GetVec3FromReadOnlySpan(ReadOnlySpan<float> a) => a.Length switch
    {
        0 => default,
        1 => new(a[0], 0, 0),
        2 => new(a[0], a[1], 0),
        _ => new(a[0], a[1], a[2])
    };
}
