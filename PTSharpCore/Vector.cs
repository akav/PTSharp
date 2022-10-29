using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Vector
    {
        
        public static Vector ORIGIN = new Vector(0, 0, 0);

        public double x, y, z, w;
        
        public Vector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            w = 1.0;
            Index = 0;

        }
        public Vector(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            Index = 0;
        }

        public const int MinimumDataLength = 4;
        public const string Prefix = "v";

        public int Index { get; internal set; }

        // Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator +(Vector a, Vector v)
        {
            return new Vector(a.x + v.x, a.y + v.y, a.z + v.z, a.w + v.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator -(Vector a, Vector v)
        {
            return new Vector(a.x - v.x, a.y - v.y, a.z - v.z, a.w - v.w);
        }

        // Dot product
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static double operator *(Vector a, Vector v)
        {
            return (a.x * v.x) + (a.y * v.y) + (a.z * v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator *(double c, Vector v)
        {
            return new Vector(c * v.x, c * v.y, c * v.z, v.w);
        }

        // Cross product
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator ^(Vector a, Vector v)
        {
            return new Vector(a.y * v.z - a.z * v.y, a.z * v.x - a.x * v.z, a.x * v.y - a.y * v.x, a.w);
        }

        // Componentwise Multiply
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator %(Vector a, Vector v)
        {
            return new Vector(a.x * v.x, a.y * v.y, a.z * v.z, a.w * v.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator *(Vector a, double c)
        {
            return new Vector(c * a.x, c * a.y, c * a.z, a.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator /(Vector a, double c)
        {
            return new Vector(a.x / c, a.y / c, a.z / c, a.w);
        }

        // Unary Minus
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator -(Vector v)
        {
            return new Vector(-v.x, -v.y, -v.z, v.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Vector operator +(Vector v)
        {
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector RandomUnitVector(Random rand)
        {
            var z = rand.NextDouble() * 2.0 - 1.0;
            var a = rand.NextDouble() * 2.0 * Math.PI;
            var r = Math.Sqrt(1.0 - z * z);
            var x = Math.Sin(a);
            var y = Math.Cos(a);
            return new Vector(r * x, r * y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(x * x + y * y + z * z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthN(double n)
        {
            if (n == 2)
            {
                return Length();
            }
            var a = Abs();
            return Math.Pow(Math.Pow(a.x, n) + Math.Pow(a.y, n) + Math.Pow(a.z, n), 1 / n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Vector b)
        {
            return x * b.x + y * b.y + z * b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Cross(Vector b)
        {
            var x = this.y * b.z - this.z * b.y;
            var y = this.z * b.x - this.x * b.z;
            var z = this.x * b.y - this.y * b.x;
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Normalize()
        {
            var d = Length();
            return new Vector(x / d, y / d, z / d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Negate()
        {
            return new Vector(-x, -y, -z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Vector Abs()
        {
            return new Vector(Math.Abs(x), Math.Abs(y), Math.Abs(z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Add(Vector b) => new Vector(x + b.x, y + b.y, z + b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Sub(Vector b) => new Vector(x - b.x, y - b.y, z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mul(Vector b) => new Vector(x * b.x, y * b.y, z * b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Div(Vector b) => new Vector(x / b.x, y / b.y, z / b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mod(Vector b)
        {
            var x = this.x - b.x * Math.Floor(this.x / b.x);
            var y = this.y - b.y * Math.Floor(this.y / b.y);
            var z = this.z - b.z * Math.Floor(this.z / b.z);
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector AddScalar(double b) => new Vector(x + b, y + b, z + b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector SubScalar(double b) => new Vector(x - b, y - b, z - b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulScalar(double b) => new Vector(x * b, y * b, z * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector DivScalar(double b) => new Vector(x / b, y / b, z / b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Min(Vector b) => new Vector(Math.Min(x, b.x), Math.Min(y, b.y), Math.Min(z, b.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Max(Vector b) => new Vector(Math.Max(x, b.x), Math.Max(y, b.y), Math.Max(z, b.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MinAxis()
        {
            (var x, var y, var z) = (Math.Abs(this.x), Math.Abs(this.y), Math.Abs(this.z));
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
        public double MinComponent() => Math.Min(Math.Min(x, y), z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MaxComponent() => Math.Max(Math.Max(x, y), z);

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
    };
}
