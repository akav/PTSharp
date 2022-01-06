using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    internal struct IVector<T> where T : struct
    {
        public Vector<T> dv;

        public IVector(T[] dArray)
        {
            dv = new Vector<T>(dArray);
        }

        public IVector(Vector<T> dv)
        {
            this.dv = dv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IVector<double> RandomUnitVector()
        {
            double z = Random.Shared.NextDouble() * 2.0d - 1.0d;
            double a = Random.Shared.NextDouble() * 2.0d * Math.PI;
            double r = Math.Sqrt(1.0d - z * z);
            double x = Math.Sin(a);
            double y = Math.Cos(a);
            return new IVector<double>(new double[] { r * x, r * y, z, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt((dynamic)dv[0] * dv[0] + (dynamic)dv[1] * dv[1] + (dynamic)dv[2] * dv[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthN(double n)
        {
            if (n == 2)
            {
                return Length();
            }
            var a = Abs();
            return Math.Pow(Math.Pow(a.dv[0], n) + Math.Pow(a.dv[1], n) + Math.Pow(a.dv[2], n), 1 / n);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthN(T n)
        {
            if ((dynamic)n == 2)
            {
                return Length();
            }
            var a = Abs();
            return Math.Pow(Math.Pow((dynamic)a.dv[0], (dynamic)n) + Math.Pow((dynamic)a.dv[1], (dynamic)n) + Math.Pow((dynamic)a.dv[2], (dynamic)n), 1 / (dynamic)n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(IVector<double> b)
        {
            return (dynamic)dv[0] * b.dv[0] + (dynamic)dv[1] * b.dv[1] + (dynamic)dv[2] * b.dv[2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Cross(IVector<T> b)
        {
            var x = (dynamic)dv[1] * b.dv[2] - (dynamic)dv[2] * b.dv[1];
            var y = (dynamic)dv[2] * b.dv[0] - (dynamic)dv[0] * b.dv[2];
            var z = (dynamic)dv[0] * b.dv[1] - (dynamic)dv[1] * b.dv[0];
            var w = (dynamic)0;

            return new IVector<double>(new double[] { x, y, z, w });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Normalize()
        {
            var d = Length();
            return new IVector<double>(new double[] { (dynamic)dv[0] / d, (dynamic)dv[1] / d, (dynamic)dv[2] / d, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Negate()
        {
            return new IVector<double>(-(dynamic)dv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IVector<double> Abs()
        {
            return new IVector<double>(new double[] { Math.Abs((dynamic)dv[0]), Math.Abs((dynamic)dv[1]), Math.Abs((dynamic)dv[2]), 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<T> Add(IVector<T> b)
        {
            return new IVector<T>(dv + b.dv);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<T> Sub(IVector<T> b)
        {
            return new IVector<T>(dv - b.dv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<T> Mul(IVector<T> b)
        {
            return new IVector<T>(dv * b.dv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<T> Div(IVector<T> b)
        {
            return new IVector<T>(dv / b.dv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Mod(IVector<T> b)
        {
            var x = dv[0] - b.dv[0] * Math.Floor((dynamic)dv[0] / b.dv[0]);
            var y = dv[1] - b.dv[1] * Math.Floor((dynamic)dv[1] / b.dv[1]);
            var z = dv[2] - b.dv[2] * Math.Floor((dynamic)dv[2] / b.dv[2]);
            return new IVector<double>(new double[] { x, y, z, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> AddScalar(double b)
        {
            return new IVector<double>(new double[] { (dynamic)dv[0] + b, (dynamic)dv[1] + b, (dynamic)dv[2] + b, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> SubScalar(double b)
        {
            return new IVector<double>(new double[] { (dynamic)dv[0] - b, (dynamic)dv[1] - b, (dynamic)dv[2] - b, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> MulScalar(double b)
        {
            return new IVector<double>(new double[] { (dynamic)dv[0] * b, (dynamic)dv[1] * b, (dynamic)dv[2] * b, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> DivScalar(double b)
        {
            return new IVector<double>(new double[] { (dynamic)dv[0] / b, (dynamic)dv[1] / b, (dynamic)dv[2] / b, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Min(IVector<double> b)
        {
            return new IVector<double>(new double[] { Math.Min((dynamic)dv[0], b.dv[0]),
                                                      Math.Min((dynamic)dv[1], b.dv[1]),
                                                      Math.Min((dynamic)dv[2], b.dv[2]),
                                                      0
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Max(IVector<double> b)
        {
            return new IVector<double>(new double[] { Math.Max((dynamic)dv[0], b.dv[0]),
                                                      Math.Max((dynamic)dv[1], b.dv[1]),
                                                      Math.Max((dynamic)dv[2], b.dv[2]),
                                                      0
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> MinAxis()
        {
            (var x, var y, var z) = (Math.Abs((dynamic)dv[0]), Math.Abs((dynamic)dv[1]), Math.Abs((dynamic)dv[2]));

            if (x <= y && x <= z)
            {
                return new IVector<double>(new double[] { 1, 0, 0, 0 });
            }
            if (y <= x && y <= z)
            {
                return new IVector<double>(new double[] { 0, 1, 0, 0 });
            }
            return new IVector<double>(new double[] { 0, 0, 1, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MinComponent()
        {
            return Math.Min(Math.Min((dynamic)dv[0], (dynamic)dv[1]), (dynamic)dv[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MaxComponent()
        {
            return Math.Max(Math.Max((dynamic)dv[0], (dynamic)dv[1]), (dynamic)dv[2]);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Reflect(IVector<double> i)
        {
            return i.Sub(MulScalar(2 * Dot(i)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> Refract(IVector<double> i, double n1, double n2)
        {
            var nr = n1 / n2;
            var cosI = -Dot(i);
            var sinT2 = nr * nr * (1 - cosI * cosI);

            if (sinT2 > 1)
            {
                return new IVector<double>(new double[] { });
            }

            var cosT = Math.Sqrt(1 - sinT2);

            return i.MulScalar(nr).Add(MulScalar(nr * cosI - cosT));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Reflectance(IVector<double> i, double n1, double n2)
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
