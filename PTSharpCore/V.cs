using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct V
    {
        public static Vector3 ORIGIN = new Vector3(0, 0, 0);
        internal Vector3 v;
        float w;
                
        public V(float X, float Y, float Z)
        {
            v = new Vector3(X, Y, Z);
            w = 1;        
        }

        public V(float X, float Y, float Z, float W)
        {
            v = new Vector3(X,Y,Z);
            w = W;        
        }

        public V(Vector3 vec)
        {
            v = vec;
            w = 1;
        
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

        public static float operator *(V a, V v)
        {
            return (a.v.X * v.v.X) + (a.v.Y * v.v.Y) + (a.v.Z * v.v.Z);
        }

        public static V operator *(float c, V v)
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

        public static V operator *(V a, float c)
        {
            return new V(c * a.v.X, c * a.v.Y, c * a.v.Z);
        }

        public static V operator /(V a, float c)
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
            float z = Random.Shared.NextSingle() * 2.0f - 1.0f;
            float a = Random.Shared.NextSingle() * 2.0f * MathF.PI;
            float r = MathF.Sqrt(1.0f - z * z);
            float x = MathF.Sin(a);
            float y = MathF.Cos(a);
            return new V(r * x, r * y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Length() => MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float LengthN(float n)
        {
            if (n == 2)
            {
                return Length();
            }
            var a = Abs();
            return MathF.Pow(MathF.Pow(a.v.X, n) + MathF.Pow(a.v.Y, n) + MathF.Pow(a.v.Z, n), 1 / n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Dot(V b)
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
            return new V(Vector3.Abs(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Add(V b)
        {
            return new V(Vector3.Add(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Sub(V b)
        {
            return new V(Vector3.Subtract(v, b.v));
        }
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Mul(V b)
        {
            return new V(Vector3.Multiply(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Div(V b)
        {
            return new V(Vector3.Divide(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Mod(V b)
        {
            var x = v.X - b.v.X * MathF.Floor(v.X / b.v.X);
            var y = v.Y - b.v.Y * MathF.Floor(v.Y / b.v.Y);
            var z = v.Z - b.v.Z * MathF.Floor(v.Z / b.v.Z);
            return new V(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V AddScalar(float b) => new V(v.X + b, v.Y + b, v.Z + b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V SubScalar(float b) => new V(v.X - b, v.Y - b, v.Z - b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MulScalar(float b) => new V(v.X * b, v.Y * b, v.Z * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V DivScalar(float b)
        {
            return new V(v.X / b, v.Y / b, v.Z / b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Min(V b)
        {
            return new V(Vector3.Min(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Max(V b)
        {
            return new V(Vector3.Max(v, b.v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MinAxis()
        {
            (var x, var y, var z) = (MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
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
        public float MinComponent() => MathF.Min(MathF.Min(v.X, v.Y), v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float MaxComponent() => MathF.Max(MathF.Max(v.X, v.Y), v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Reflect(V i) => i.Sub(MulScalar(2 * Dot(i)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V Refract(V i, float n1, float n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return new V();
            }
            
            var cosT = MathF.Sqrt(1 - sinT2);

            return i.MulScalar(nr).Add(MulScalar(nr * cosI - cosT));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Reflectance(V i, float n1, float n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if(sinT2 > 1)
            {
                return 1;
            }
            
            var cosT = MathF.Sqrt(1 - sinT2);
            var rOrth = (n1 * cosI - n2 * cosT) / (n1 * cosI + n2 * cosT);
            var rPar = (n2 * cosI - n1 * cosT) / (n2 * cosI + n1 * cosT);
            return (rOrth * rOrth + rPar * rPar) / 2;
        }
    };
}