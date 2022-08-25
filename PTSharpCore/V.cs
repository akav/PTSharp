using System;
using System.DoubleNumerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct V
    {
        public static Vector3 ORIGIN = new Vector3(0, 0, 0);
        internal Vector4 v;
        double w;
                
        public V(double X, double Y, double Z)
        {
            v = new Vector4(X, Y, Z, 1);        
        }

        public V(double X, double Y, double Z, double W)
        {
            v = new Vector4(X,Y,Z,W);        
        }

        public V(Vector4 vec)
        {
            v = vec;
        
        }

        public const int MinimumDataLength = 4;
        public const string Prefix = "v";
        
        public static V operator +(V a, V v)
        {
            return new V(a.v + v.v);
        }

        public static V operator -(V a, V v)
        {
            return new V(a.v - v.v);
        }

        public static double operator *(V a, V v)
        {
            return (a.v.X * v.v.X) + (a.v.Y * v.v.Y) + (a.v.Z * v.v.Z);
        }

        public static V operator *(double c, V v)
        {
            return new V(c * v.v.X, c * v.v.Y, c * v.v.Z);
        }

        public static V operator ^(V a, V v)
        {
            return new V(a.v.Y * v.v.Z - a.v.Z * v.v.Y, a.v.Z * v.v.X - a.v.X * v.v.Z, a.v.X * v.v.Y - a.v.Y * v.v.X);
        }

        public static V operator %(V a, V v)
        {
            return new V(a.v.X * v.v.X, a.v.Y * v.v.Y, a.v.Z * v.v.Z);
        }

        public static V operator *(V a, double c)
        {
            return new V(c * a.v.X, c * a.v.Y, c * a.v.Z);
        }

        public static V operator /(V a, double c)
        {
            return new V(a.v.X / c, a.v.Y / c, a.v.Z / c);
        }

        public static V operator -(V v)
        {
            return new V(-v.v.X, -v.v.Y, -v.v.Z);
        }

        public static V operator +(V v)
        {
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V RandomUnitVector()
        {
            double z = Random.Shared.NextDouble() * 2.0f - 1.0f;
            double a = Random.Shared.NextDouble() * 2.0f * Math.PI;
            double r = Math.Sqrt(1.0f - z * z);
            double x = Math.Sin(a);
            double y = Math.Cos(a);
            return new V(r * x, r * y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthN(double n)
        {
            if (n == 2)
            {
                return Length();
            }
            var a = Abs();
            return Math.Pow(Math.Pow(a.v.X, n) + Math.Pow(a.v.Y, n) + Math.Pow(a.v.Z, n), 1 / n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(V b)
        {
            return v.X * b.v.X + v.Y * b.v.Y + v.Z * b.v.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Cross(V b)
        {
            var x = v.Y * b.v.Z - v.Z * b.v.Y;
            var y = v.Z * b.v.X - v.X * b.v.Z;
            var z = v.X * b.v.Y - v.Y * b.v.X;
            return new V(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Normalize()
        {
            var d = Length();
            return new V(v.X / d, v.Y / d, v.Z / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Negate()
        {
            return new V(-v.X, -v.Y, -v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        V Abs()
        {
            return new V(Vector4.Abs(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Add(V b)
        {
            return new V(Vector4.Add(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Sub(V b)
        {
            return new V(Vector4.Subtract(v, b.v));
        }
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Mul(V b)
        {
            return new V(Vector4.Multiply(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Div(V b)
        {
            return new V(Vector4.Divide(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Mod(V b)
        {
            var x = v.X - b.v.X * Math.Floor(v.X / b.v.X);
            var y = v.Y - b.v.Y * Math.Floor(v.Y / b.v.Y);
            var z = v.Z - b.v.Z * Math.Floor(v.Z / b.v.Z);
            return new V(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V AddScalar(double b) => new V(v.X + b, v.Y + b, v.Z + b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V SubScalar(double b) => new V(v.X - b, v.Y - b, v.Z - b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MulScalar(double b) => new V(v.X * b, v.Y * b, v.Z * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V DivScalar(double b)
        {
            return new V(v.X / b, v.Y / b, v.Z / b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Min(V b)
        {
            return new V(Vector4.Min(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Max(V b)
        {
            return new V(Vector4.Max(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MinAxis()
        {
            (var x, var y, var z) = (Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
            if (x <= y && x <= z)
            {
                return new V(1, 0, 0);
            }
            if (y <= x && y <= z)
            {
                return new V(0, 1, 0);
            }
            return new V(0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MinComponent() => Math.Min(Math.Min(v.X, v.Y), v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MaxComponent() => Math.Max(Math.Max(v.X, v.Y), v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Reflect(V i) => i.Sub(MulScalar(2 * Dot(i)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Refract(V i, double n1, double n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return new V();
            }
            
            var cosT = Math.Sqrt(1 - sinT2);

            return i.MulScalar(nr).Add(MulScalar(nr * cosI - cosT));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Reflectance(V i, double n1, double n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if(sinT2 > 1)
            {
                return 1;
            }
            
            var cosT = Math.Sqrt(1 - sinT2);
            var rOrth = (n1 * cosI - n2 * cosT) / (n1 * cosI + n2 * cosT);
            var rPar = (n2 * cosI - n1 * cosT) / (n2 * cosI + n1 * cosT);
            return (rOrth * rOrth + rPar * rPar) / 2;
        }
    };
}