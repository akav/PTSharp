using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    // Referenced Vector2 class from https://github.com/merwaaan/pbrt/blob/master/pbrt/core/geometry/Vector.cs
    public class Vector2<T>
    {
        public T X;
        public T Y;

        public static Vector2<T> Zero => new Vector2<T>((dynamic)0, (dynamic)0);
        public static Vector2<T> One => new Vector2<T>((dynamic)1, (dynamic)1);

        public Vector2()
        {
        }

        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector2<T> v)
        {
            X = v.X;
            Y = v.Y;
        }

        public static Vector2<T> operator +(Vector2<T> a, Vector2<T> b)
        {
            return new Vector2<T>((dynamic)a.X + b.X, (dynamic)a.Y + b.Y);
        }

        public static Vector2<T> operator -(Vector2<T> a, Vector2<T> b)
        {
            return new Vector2<T>((dynamic)a.X - b.X, (dynamic)a.Y - b.Y);
        }

        public static Vector2<T> operator *(Vector2<T> a, T s)
        {
            return new Vector2<T>((dynamic)a.X * s, (dynamic)a.Y * s);
        }

        public float LengthSquared()
        {
            return (dynamic)X * X + (dynamic)Y * Y;
        }

        public float Length()
        {
            return MathF.Sqrt(LengthSquared());
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    // Referenced Vector3 class from https://github.com/merwaaan/pbrt/blob/master/pbrt/core/geometry/Vector.cs
    public class Vector3<T>
    {
        public T X;
        public T Y;
        public T Z;

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Vector3<T> Zero => new Vector3<T>((dynamic)0, (dynamic)0, (dynamic)0);
        public static Vector3<T> One => new Vector3<T>((dynamic)1, (dynamic)1, (dynamic)1);

        public Vector3()
        {
        }

        public Vector3(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector3<T> v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3(Point3<T> p)
        {
            X = p.X;
            Y = p.Y;
            Z = p.Z;
        }

        public static Vector3<T> operator +(Vector3<T> a, Vector3<T> b)
        {
            return new Vector3<T>((dynamic)a.X + b.X, (dynamic)a.Y + b.Y, (dynamic)a.Z + b.Z);
        }

        public static Vector3<T> operator -(Vector3<T> a, Vector3<T> b)
        {
            return new Vector3<T>((dynamic)a.X - b.X, (dynamic)a.Y - b.Y, (dynamic)a.Z - b.Z);
        }

        public static Vector3<T> operator *(Vector3<T> a, T s)
        {
            return new Vector3<T>((dynamic)a.X * s, (dynamic)a.Y * s, (dynamic)a.Z * s);
        }

        public static Vector3<T> operator -(Vector3<T> a)
        {
            return a.Negated();
        }

        public float LengthSquared()
        {
            return (dynamic)X * X + (dynamic)Y * Y + (dynamic)Z * Z;
        }

        public float Length()
        {
            return MathF.Sqrt(LengthSquared());
        }

        public float Dot(Vector3<T> v)
        {
            return (dynamic)X * v.X + (dynamic)Y * v.Y + (dynamic)Z * v.Z;
        }

        public static float Dot(Vector3<T> a, Vector3<T> b)
        {
            return (dynamic)a.X * b.X + (dynamic)a.Y * b.Y + (dynamic)a.Z * b.Z;
        }

        public static float Dot(Vector3<T> a, Normal3<T> b)
        {
            return Dot(a, b.ToVector3());
        }

        public static float AbsDot(Vector3<T> a, Vector3<T> b)
        {
            return Math.Abs(Dot(a, b));
        }

        public static float AbsDot(Vector3<T> a, Normal3<T> b)
        {
            return Math.Abs(Dot(a, b));
        }

        public static Vector3<T> Cross(Vector3<T> a, Vector3<T> b)
        {
            return new Vector3<T>(
                (dynamic)a.Y * b.Z - (dynamic)a.Z * b.Y,
                (dynamic)a.Z * b.X - (dynamic)a.X * b.Z,
                (dynamic)a.X * b.Y - (dynamic)a.Y * b.X);
        }

        public Vector3<T> Normalized()
        {
            var l = Length();
            return new Vector3<T>((dynamic)X / l, (dynamic)Y / l, (dynamic)Z / l);
        }

        public Vector3<T> Negated()
        {
            return new Vector3<T>((dynamic)X * -1, (dynamic)Y * -1, (dynamic)Z * -1);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector
    {
        public static Vector ORIGIN = new Vector(0, 0, 0);
        public static Vector Zero = new Vector(0, 0, 0);
        public static Vector One = new Vector(1, 1, 1);
        public static Vector Up = new Vector(0, 1, 0);

        private Vector3 _vector;
        public double W;

        public double X
        {
            get => _vector.X;
            set => _vector.X = (float)value;
        }

        public double Y
        {
            get => _vector.Y;
            set => _vector.Y = (float)value;
        }

        public double Z
        {
            get => _vector.Z;
            set => _vector.Z = (float)value;
        }

        public Vector(double x, double y, double z)
        {
            _vector = new Vector3((float)x, (float)y, (float)z);
            W = 1.0;
            Index = 0;
        }

        public Vector(double x, double y, double z, double w)
        {
            _vector = new Vector3((float)x, (float)y, (float)z);
            W = w;
            Index = 0;
        }

        public const int MinimumDataLength = 4;
        public const string Prefix = "v";

        public int Index { get; internal set; }

        // Operators
        public static Vector operator +(Vector a, Vector v)
        {
            return new Vector(a.X + v.X, a.Y + v.Y, a.Z + v.Z, a.W + v.W);
        }

        public static Vector operator +(Vector v, double a)
        {
            return new Vector(a + v.X, a + v.Y, a + v.Z);
        }

        public static Vector operator -(Vector a, Vector v)
        {
            return new Vector(a.X - v.X, a.Y - v.Y, a.Z - v.Z, a.W - v.W);
        }

        // Dot product
        public static double operator *(Vector a, Vector v)
        {
            return Vector3.Dot(a._vector, v._vector);
        }

        public static Vector operator *(double c, Vector v)
        {
            return new Vector(c * v.X, c * v.Y, c * v.Z, v.W);
        }

        // Cross product
        public static Vector operator ^(Vector a, Vector v)
        {
            var cross = Vector3.Cross(a._vector, v._vector);
            return new Vector(cross.X, cross.Y, cross.Z, a.W);
        }

        // Componentwise Multiply
        public static Vector operator %(Vector a, Vector v)
        {
            return new Vector(a.X * v.X, a.Y * v.Y, a.Z * v.Z, a.W * v.W);
        }

        public static Vector operator *(Vector a, double c)
        {
            return new Vector(c * a.X, c * a.Y, c * a.Z, a.W);
        }

        public static Vector operator *(Vector a, System.Numerics.Vector3 v)
        {
            return new Vector(a.X * v.X, a.Y * v.Y, a.Z * v.Z);
        }

        public static Vector operator /(Vector a, double c)
        {
            return new Vector(a.X / c, a.Y / c, a.Z / c, a.W);
        }

        // Unary Minus
        public static Vector operator -(Vector v)
        {
            return new Vector(-v.X, -v.Y, -v.Z, v.W);
        }

        public static Vector operator +(Vector v)
        {
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector RandomPointOnUnitSphere()
        {
            var u = Random.Shared.NextDouble();
            var v = Random.Shared.NextDouble();
            var theta = 2 * Math.PI * u;
            var phi = Math.Acos(2 * v - 1);
            var x = Math.Sin(phi) * Math.Cos(theta);
            var y = Math.Sin(phi) * Math.Sin(theta);
            var z = Math.Cos(phi);
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector RandomPointOnUnitHemisphere(Vector normal)
        {
            // Generate a random point on the unit sphere centered at the origin
            var pointOnUnitSphere = RandomPointOnUnitSphere();

            // If the point is on the same side of the hemisphere as the normal vector, return it
            if (pointOnUnitSphere.Dot(normal) > 0)
            {
                return pointOnUnitSphere;
            }
            // Otherwise, return the point reflected about the normal vector
            else
            {
                return pointOnUnitSphere.Reflect(normal);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector RandomUnitVector(Random rand)
        {
            var z = Random.Shared.NextDouble() * 2.0 - 1.0;
            var a = Random.Shared.NextDouble() * 2.0 * Math.PI;
            var r = Math.Sqrt(1.0 - z * z);
            var x = Math.Sin(a);
            var y = Math.Cos(a);
            return new Vector(r * x, r * y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AbsDot(Vector v1, Vector v2)
        {
            return Math.Abs(Vector3.Dot(v1._vector, v2._vector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => _vector.Length();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthN(double n)
        {
            if (n == 2)
            {
                return Length();
            }
            var a = Abs();
            return Math.Pow(Math.Pow(a.X, n) + Math.Pow(a.Y, n) + Math.Pow(a.Z, n), 1 / n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Vector b)
        {
            return Vector3.Dot(_vector, b._vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector a, Vector b)
        {
            return Vector3.Dot(a._vector, b._vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Cross(Vector b)
        {
            var cross = Vector3.Cross(_vector, b._vector);
            return new Vector(cross.X, cross.Y, cross.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Normalize()
        {
            var normalized = Vector3.Normalize(_vector);
            return new Vector(normalized.X, normalized.Y, normalized.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Negate()
        {
            return new Vector(-X, -Y, -Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Vector Abs()
        {
            return new Vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Add(Vector b) => new Vector(X + b.X, Y + b.Y, Z + b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Sub(Vector b) => new Vector(X - b.X, Y - b.Y, Z - b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mul(Vector b) => new Vector(X * b.X, Y * b.Y, Z * b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Div(Vector b) => new Vector(X / b.X, Y / b.Y, Z / b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mod(Vector b)
        {
            var x = this.X - b.X * Math.Floor(this.X / b.X);
            var y = this.Y - b.Y * Math.Floor(this.Y / b.Y);
            var z = this.Z - b.Z * Math.Floor(this.Z / b.Z);
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector AddScalar(double b) => new Vector(X + b, Y + b, Z + b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector SubScalar(double b) => new Vector(X - b, Y - b, Z - b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulScalar(double b) => new Vector(X * b, Y * b, Z * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector DivScalar(double b) => new Vector(X / b, Y / b, Z / b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Min(Vector b) => new Vector(Math.Min(X, b.X), Math.Min(Y, b.Y), Math.Min(Z, b.Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Max(Vector b) => new Vector(Math.Max(X, b.X), Math.Max(Y, b.Y), Math.Max(Z, b.Z));

        public static bool operator !=(Vector a, Vector b)
        {
            return !(a == b);
        }

        public static bool operator ==(Vector a, Vector b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector)
            {
                return this == (Vector)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MinAxis()
        {
            (var x, var y, var z) = (Math.Abs(this.X), Math.Abs(this.Y), Math.Abs(this.Z));
            if (x <= y && x <= z)
            {
                return new Vector(1, 0, 0);
            }
            if (y <= x && y <= z)
            {
                return new Vector(0, 1, 0);
            }
            return new Vector(0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MinComponent() => Math.Min(Math.Min(X, Y), Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MaxComponent() => Math.Max(Math.Max(X, Y), Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Reflect(Vector i) => i.Sub(MulScalar(2 * Dot(i)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Refract(Vector i, double n1, double n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return new Vector();
            }

            var cosT = Math.Sqrt(1 - sinT2);

            return i.MulScalar(nr).Add(MulScalar(nr * cosI - cosT));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Reflectance(Vector i, double n1, double n2)
        {
            var nr2 = (n1 * n1) / (n2 * n2);  // Square of refractive index ratio
            var cosI = -Dot(i);
            var sinT2 = nr2 * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return 1;
            }

            var cosT = Math.Sqrt(1 - sinT2);

            var cosI_n1 = n1 * cosI;
            var cosT_n2 = n2 * cosT;
            var rOrth = (cosI_n1 - cosT_n2) / (cosI_n1 + cosT_n2);
            var rPar = (cosT_n2 - cosI_n1) / (cosT_n2 + cosI_n1);

            return (rOrth * rOrth + rPar * rPar) / 2;
        }

        public double DistanceTo(Vector other)
        {
            var diff = this - other;
            return Math.Sqrt(diff.Dot(diff));
        }
    }
}