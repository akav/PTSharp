using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

    public class Vector
    {
        public double X;
        public double Y;
        public double Z;

        public double this[int index]
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

        public static Vector Origin = new Vector(0, 0, 0);
        public static Vector Zero = new Vector(0, 0, 0);
        public static Vector One = new Vector(1, 1, 1);
        public static Vector Up = new Vector(0, 1, 0);
        public static Vector Right = new Vector(1, 0, 0);

        public Vector()
        {
        }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }        
        
        public Vector(Vector3<double> v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        // Operators
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector operator +(Vector v, float a)
        {
            return new Vector(a + v.X, a + v.Y, a + v.Z);
        }

        public static Vector operator -(Vector a, Vector v)
        {
            return new Vector(a.X - v.X, a.Y - v.Y, a.Z - v.Z);
        }

        public static Vector operator *(Vector a, float c)
        {
            return new Vector(c * a.X, c * a.Y, c * a.Z);
        }

        public static Vector operator *(float c, Vector a)
        {
            return new Vector(c * a.X, c * a.Y, c * a.Z);
        }

        public static Vector operator /(Vector a, float c)
        {
            return new Vector(a.X / c, a.Y / c, a.Z / c);
        }

        public static Vector operator -(Vector a)
        {
            return a.Negated();
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
            var pointOnUnitSphere = RandomPointOnUnitSphere();

            if (pointOnUnitSphere.Dot(normal) > 0)
            {
                return pointOnUnitSphere;
            }
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
        public static double AbsDot(Vector a, Vector b) => Math.Abs(Dot(a, b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSquared() => X * X + Y * Y + Z * Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Vector b) => X * b.X + Y * b.Y + Z * b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector a, Vector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Cross(Vector a, Vector b)
        {
            return new Vector(a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Normalized()
        {
            var l = Length();
            return new Vector(X / l, Y / l, Z / l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Negated()
        {
            return new Vector(-X, -Y, -Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Add(Vector b)
        {
            return new Vector(X + b.X, Y + b.Y, Z + b.Z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Sub(Vector b)
        {

            return new Vector(X - b.X, Y - b.Y, Z - b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mul(Vector b)
        {
            return new Vector(X * b.X, Y * b.Y, Z * b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Div(Vector b)
        {
            return new Vector(X / b.X, Y / b.Y, Z / b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mod(Vector b)
        {
            var x = X - b.X * Math.Floor(X / b.X);
            var y = Y - b.Y * Math.Floor(Y / b.Y);
            var z = Z - b.Z * Math.Floor(Z / b.Z);
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector AddScalar(double b)
        {
            return new Vector(X + b, Y + b, Z + b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector SubScalar(double b)
        {
            return new Vector(X - b, Y - b, Z - b);
        }

        public Vector MulScalar(double b)
        {
            return new Vector(X * b, Y * b, Z * b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector DivScalar(double b)
        {
            return new Vector(X / b, Y / b, Z / b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Min(Vector b) => new Vector(Math.Min(X, b.X), Math.Min(Y, b.Y), Math.Min(Z, b.Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Min(Vector a, Vector b)
        {
            return new Vector(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Max(Vector b) => new Vector(Math.Max(X, b.X), Math.Max(Y, b.Y), Math.Max(Z, b.Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Max(Vector a, Vector b)
        {
            return new Vector(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MinAxis()
        {
            (var x, var y, var z) = (Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
            
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

        public double MinComponent() => Math.Min(Math.Min(X, Y), Z);

        public double MaxComponent() => Math.Max(Math.Max(X, Y), Z);

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
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return 1;
            }

            var cosT = Math.Sqrt(1 - sinT2);
            var rOrth = (n1 * cosI - n2 * cosT) / (n1 * cosI + n2 * cosT);
            var rPar = (n2 * cosI - n1 * cosT) / (n2 * cosI + n1 * cosT);
            return (rOrth * rOrth + rPar * rPar) / 2;
        }

        
        public Vector2<double> ToVector2()
        {
            return new Vector2<double>(X, Y);
        }

        public Vector3<double> ToVector3()
        {
            return new Vector3<double>(X, Y, Z);
        }
                
        public static Vector Lerp(Vector a, Vector b, float t)
        {
            return a.Add(b.Sub(a).MulScalar(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Slerp(Vector a, Vector b, float t)
        {
            var dot = Dot(a, b);
            dot = Math.Clamp(dot, -1, 1);
            var theta = Math.Acos(dot) * t;
            var relativeVec = b.Sub(a.MulScalar(dot));
            relativeVec = relativeVec.Normalized();
            return a.MulScalar(Math.Cos(theta)).Add(relativeVec.MulScalar(Math.Sin(theta)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Slerp(Vector a, Vector b, float t, float omega, bool allowFlip)
        {
            var dot = Dot(a, b);
            var sign = 1.0f;
            if (dot < 0 && allowFlip)
            {
                dot = -dot;
                sign = -1.0f;
            }
            dot = Math.Clamp(dot, -1, 1);
            var theta = Math.Acos(dot) * t;
            var relativeVec = b.Sub(a.MulScalar(dot));
            relativeVec = relativeVec.Normalized();
            return a.MulScalar(Math.Cos(theta)).Add(relativeVec.MulScalar(Math.Sin(theta) * sign));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Normalize()
        {
            var d = Length();
            return new Vector(X / d, Y / d, Z / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Negate()
        {
            return new Vector(-X, -Y, -Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Cross(Vector b)
        {
            var x = Y * b.Z - Z * b.Y;
            var y = Z * b.X - X * b.Z;
            var z = X * b.Y - Y * b.X;
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

        
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
        Vector Abs()
        {
            return new Vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }
    }
}
