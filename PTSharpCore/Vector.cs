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

    [StructLayout(LayoutKind.Sequential, Pack = 128)]
    public struct Vector 
    {
        private Vector256<double> data;

        public double X
        {
            get => data.GetElement(0);
            set
            {
                data = Vector256.Create(value, Y, Z, W);
            }
        }

        public double Y
        {
            get => data.GetElement(1);
            set
            {
                data = Vector256.Create(X, value, Z, W);
            }
        }

        public double Z
        {
            get => data.GetElement(2);
            set
            {
                data = Vector256.Create(X, Y, value, W);
            }
        }

        public double W
        {
            get => data.GetElement(3);
            set
            {
                data = Vector256.Create(X, Y, Z, value);
            }
        }

        public static Vector Origin = new Vector(0, 0, 0);
        public static Vector Zero = new Vector(0, 0, 0);
        public static Vector One = new Vector(1, 1, 1);
        public static Vector Up = new Vector(0, 1, 0);
        public static Vector Right = new Vector(1, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector(Vector256<double> data)
        {
            this.data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(double x, double y, double z, double w = 1.0)
        {
            data = Vector256.Create(x, y, z, w);
        }        
        
        public Vector() { }

        // Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(Vector256.Add(a.data, b.data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator +(Vector v, double a)
        {
            var scalarVector = Vector256.Create(a);
            var result = Vector256.Add(v.data, scalarVector);
            return new Vector(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator -(Vector a, Vector v)
        {
            var result = Vector256.Subtract(a.data, v.data);
            return new Vector(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(Vector a, Vector v)
        {
            var mulResult = Vector256.Multiply(a.data, v.data);
            double dotProduct = mulResult.GetElement(0) + mulResult.GetElement(1) + mulResult.GetElement(2);
            return dotProduct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator *(double c, Vector v)
        {
            var scalarVector = Vector256.Create(c);
            var result = Vector256.Multiply(scalarVector, v.data);
            return new Vector(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator *(Vector a, double c)
        {
            return new Vector(c * a.X, c * a.Y, c * a.Z, a.W);
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
        public static double AbsDot(Vector v1, Vector v2)
        {
            var mulResult = Vector256.Multiply(v1.data, v2.data);
            double dotProduct = mulResult.GetElement(0) + mulResult.GetElement(1) + mulResult.GetElement(2);
            var absDotProduct = Abs(Vector256.Create(dotProduct));
            return absDotProduct.data.GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() //=> Math.Sqrt(X * X + Y * Y + Z * Z);
        {
            var squares = Vector256.Multiply(data, data);
            double sumOfSquares = squares.GetElement(0) + squares.GetElement(1) + squares.GetElement(2);
            var sqrtSumOfSquares = Avx.Sqrt(Vector256.Create(sumOfSquares));
            return sqrtSumOfSquares.GetElement(0);
        }
    

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
            var product = data * b.data;
            double dotProduct = product.GetElement(0) + product.GetElement(1) + product.GetElement(2);
            return dotProduct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static double Dot(Vector a, Vector b)
        {
            var product = Vector256.Multiply(a.data, b.data);
            double dotProduct = product.GetElement(0) + product.GetElement(1) + product.GetElement(2);
            return dotProduct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Cross(Vector b)
        {
            var x = this.Y * b.Z - this.Z * b.Y;
            var y = this.Z * b.X - this.X * b.Z;
            var z = this.X * b.Y - this.Y * b.X;
            return new Vector(x, y, z);
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
        Vector Abs()
        {
            return new Vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Abs(Vector256<double> value)
        {
            var mask = Vector256.Create(0x7FFFFFFFFFFFFFFF).AsDouble();
            return new Vector(Avx.And(value, mask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Add(Vector b)
        {
            return new Vector(Avx2.Add(this.data, b.data));            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Sub(Vector b)
        {
            return new Vector(Avx2.Subtract(this.data, b.data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Mul(Vector b)
        {
            return new Vector(Avx2.Multiply(this.data, b.data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Div(Vector b)
        {
            return new Vector(Avx2.Divide(this.data, b.data));
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
            var scalar = Vector256.Create(b);
            return new Vector(Avx2.Add(this.data, scalar));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector SubScalar(double b) 
        {
            var scalar = Vector256.Create(b);
            return new Vector(Avx2.Subtract(this.data, scalar));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulScalar(double b) 
        {
            var scalar = Vector256.Create(b);
            return new Vector(Avx2.Multiply(this.data, scalar));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector DivScalar(double b)
        {
            var scalar = Vector256.Create(b);
            return new Vector(Avx2.Divide(this.data, scalar));
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
    }
}
