using PTSharpCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    interface SDF
    {
        double Evaluate(V p);
        Box BoundingBox();
    }

    class SDFShape : IShape, SDF
    {
        SDF SDF;
        Material Material;

        SDFShape(SDF sdf, Material material)
        {
            SDF = sdf;
            Material = material;
        }

        internal static IShape NewSDFShape(SDF sdf, Material material)
        {
            return new SDFShape(sdf, material);
        }

        public void Compile() { }

        Hit IShape.Intersect(Ray ray)
        {
            double epsilon = 0.00001F;
            double start = 0.0001F;
            double jumpSize = 0.001F;
            Box box = BoundingBox();
            (double t1, double t2) = box.Intersect(ray);
            if (t2 < t1 || t2 < 0)
            {
                return Hit.NoHit;
            }

            double t = Math.Max(start, t1);
            bool jump = true;

            for (int i = 0; i < 1000; i++)
            {
                var d = Evaluate(ray.Position(t));
                if (jump && d < 0)
                {
                    t -= jumpSize;
                    jump = false;
                    continue;
                }

                if (d < epsilon)
                {
                    return new Hit(this, t, null);
                }

                if (jump && d < jumpSize)
                {
                    d = jumpSize;
                }

                t += d;

                if (t > t2)
                {
                    return Hit.NoHit;

                }
            }
            return Hit.NoHit;
        }

        V IShape.UV(V uv)
        {
            return new V();
        }

        V IShape.NormalAt(V p)
        {
            double e = 0.0001F;
            (var x, var y, var z) = (p.v.X, p.v.Y, p.v.Z);

            var n = new V(Evaluate(new V(x - e, y, z)) - Evaluate(new V(x + e, y, z)),
                               Evaluate(new V(x, y - e, z)) - Evaluate(new V(x, y + e, z)),
                               Evaluate(new V(x, y, z - e)) - Evaluate(new V(x, y, z + e)));
            return n.Normalize();
        }

        Material IShape.MaterialAt(V v)
        {
            return Material;
        }

        public Box BoundingBox()
        {
            return SDF.BoundingBox();
        }

        public double Evaluate(V p)
        {
            return SDF.Evaluate(p);
        }
    }

    internal class SphereSDF : SDF
    {
        double Radius;
        double Exponent;

        SphereSDF(double Radius, double Exponent)
        {
            this.Radius = Radius;
            this.Exponent = Exponent;
        }
        internal static SDF NewSphereSDF(double radius)
        {
            return new SphereSDF(radius, 2);
        }

        double SDF.Evaluate(V p)
        {
            return p.LengthN(Exponent) - Radius;
        }

        Box SDF.BoundingBox()
        {
            double r = Radius;
            return new Box(new V(-r, -r, -r), new V(r, r, r));
        }
    }

    internal class CubeSDF : SDF
    {
        V Size;

        CubeSDF(V size)
        {
            Size = size;
        }

        internal static SDF NewCubeSDF(V size)
        {
            return new CubeSDF(size);
        }

        double SDF.Evaluate(V p)
        {
            double x = p.v.X;
            double y = p.v.Y;
            double z = p.v.Z;

            if (x < 0)
                x = -x;
            if (y < 0)
                y = -y;
            if (z < 0)
                z = -z;

            x -= Size.v.X / 2;
            y -= Size.v.Y / 2;
            z -= Size.v.Z / 2;

            double a = x;
            if (y > a)
                a = y;
            if (z > a)
                a = z;
            if (a > 0)
                a = 0;
            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;
            if (z < 0)
                z = 0;
            double b = Math.Sqrt(x * x + y * y + z * z);
            return a + b;
        }

        Box SDF.BoundingBox()
        {
            (var x, var y, var z) = (Size.v.X / 2, Size.v.Y / 2, Size.v.Z / 2);
            return new Box(new V(-x, -y, -z), new V(x, y, z));
        }
    }

    internal class CylinderSDF : SDF
    {
        double Radius;
        double Height;

        CylinderSDF(double Radius, double Height)
        {
            this.Radius = Radius;
            this.Height = Height;
        }

        internal static SDF NewCylinderSDF(double radius, double height)
        {
            return new CylinderSDF(radius, height);
        }

        internal Box BoundingBox()
        {
            double r = Radius;
            double h = Height / 2;
            return new Box(new V(-r, -h, -r), new V(r, h, r));
        }
        Box SDF.BoundingBox()
        {
            double r = Radius;
            double h = Height / 2;
            return new Box(new V(-r, -h, -r), new V(r, h, r));
        }

        double SDF.Evaluate(V p)
        {
            double x = Math.Sqrt(p.v.X * p.v.X + p.v.Z * p.v.Z);
            double y = p.v.Y;

            if (x < 0)
                x = -x;
            if (y < 0)
                y = -y;
            x -= Radius;
            y -= Height / 2;

            double a = x;

            if (y > a)
                a = y;
            if (a > 0)
                a = 0;
            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;

            double b = Math.Sqrt(x * x + y * y);
            return a + b;
        }
    }

    class CapsuleSDF : SDF
    {
        V A, B;
        double Radius;
        double Exponent;

        CapsuleSDF(V A, V B, double Radius, double Exponent)
        {
            this.A = A;
            this.B = B;
            this.Radius = Radius;
            this.Exponent = Exponent;
        }
        internal static SDF NewCapsuleSDF(V a, V b, double radius)
        {
            return new CapsuleSDF(a, b, radius, 2);
        }

        double SDF.Evaluate(V p)
        {
            var pa = p.Sub(A);
            var ba = B.Sub(A);
            var h = Math.Max(0, Math.Min(1, pa.Dot(ba) / ba.Dot(ba)));
            return pa.Sub(ba.MulScalar(h)).LengthN(Exponent) - Radius;
        }

        Box SDF.BoundingBox()
        {
            (var a, var b) = (A.Min(B), A.Max(B));
            return new Box(a.SubScalar(Radius), b.AddScalar(Radius));
        }
    }

    class TorusSDF : SDF
    {
        double MajorRadius;
        double MinRadius;
        double MajorExponent;
        double MinorExponent;

        TorusSDF(double MajorRadius, double MinRadius, double MajorExponent, double MinorExponent)
        {
            this.MajorRadius = MajorRadius;
            this.MinRadius = MinRadius;
            this.MajorExponent = MajorExponent;
            this.MinorExponent = MinorExponent;
        }

        internal static SDF NewTorusSDF(double major, double minor)
        {
            return new TorusSDF(major, minor, 2, 2);
        }

        double SDF.Evaluate(V p)
        {
            V q = new V(new V(p.v.X, p.v.Y, 0).LengthN(MajorExponent) - MajorRadius, p.v.Z, 0);
            return q.LengthN(MinorExponent) - MinRadius;
        }

        Box SDF.BoundingBox()
        {
            double a = MinRadius;
            double b = MinRadius + MajorRadius;
            return new Box(new V(-b, -b, a), new V(b, b, a));
        }
    }

    internal class TransformSDF : SDF
    {
        SDF SDF;
        Matrix Matrix;
        Matrix Inverse;

        TransformSDF(SDF SDF, Matrix Matrix, Matrix Inverse)
        {
            this.SDF = SDF;
            this.Matrix = Matrix;
            this.Inverse = Inverse;
        }

        internal static SDF NewTransformSDF(SDF sdf, Matrix matrix)
        {
            return new TransformSDF(sdf, matrix, matrix.Inverse());
        }

        double SDF.Evaluate(V p)
        {
            var q = Inverse.MulPosition(p);
            return SDF.Evaluate(q);
        }
        internal Box BoundingBox()
        {
            return Matrix.MulBox(SDF.BoundingBox());
        }

        Box SDF.BoundingBox()
        {
            return Matrix.MulBox(SDF.BoundingBox());
        }
    }

    class ScaleSDF : SDF
    {
        SDF SDF;
        double Factor;

        ScaleSDF(SDF sdf, double Factor)
        {
            SDF = sdf;
            this.Factor = Factor;
        }

        internal static SDF NewScaleSDF(SDF sdf, double factor)
        {
            return new ScaleSDF(sdf, factor);
        }

        double SDF.Evaluate(V p)
        {
            return SDF.Evaluate(p.DivScalar(Factor)) * Factor;
        }

        Box SDF.BoundingBox()
        {
            double f = Factor;
            Matrix m = new Matrix().Scale(new V(f, f, f));
            return m.MulBox(SDF.BoundingBox());
        }
    }

    class UnionSDF : SDF
    {
        SDF[] Items;

        UnionSDF(SDF[] Items)
        {
            this.Items = Items;
        }

        internal static SDF NewUnionSDF(SDF[] items)
        {
            return new UnionSDF(items);
        }

        double SDF.Evaluate(V p)
        {
            double result = 0;
            int i = 0;
            foreach (SDF item in Items)
            {
                double d = item.Evaluate(p);
                if (i == 0 || d < result)
                {
                    result = d;
                }
                i++;
            }
            return result;
        }

        Box SDF.BoundingBox()
        {
            Box result = new Box();
            Box box;
            int i = 0;

            foreach (SDF item in Items)
            {
                box = item.BoundingBox();
                if (i == 0)
                {
                    result = box;
                }
                else
                {
                    result = result.Extend(box);
                }
                i++;
            }
            return result;
        }
    }

    internal class DifferenceSDF : SDF
    {
        SDF[] Items;

        DifferenceSDF(SDF[] Items)
        {
            this.Items = Items;
        }

        internal static SDF NewDifferenceSDF(List<SDF> items)
        {
            return new DifferenceSDF(items.ToArray());
        }


        double SDF.Evaluate(V p)
        {
            double result = 0;
            int i = 0;

            foreach (SDF item in Items)
            {
                double d = item.Evaluate(p);
                if (i == 0)
                {
                    result = d;
                }
                else if (-d > result)
                {
                    result = -d;
                }
                i++;
            }
            return result;
        }

        Box SDF.BoundingBox()
        {
            return Items[0].BoundingBox();
        }
    }

    internal class IntersectionSDF : SDF
    {
        SDF[] Items;

        IntersectionSDF(SDF[] Items)
        {
            this.Items = Items;
        }

        internal static SDF NewIntersectionSDF(List<SDF> items)
        {
            return new IntersectionSDF(items.ToArray());
        }

        double SDF.Evaluate(V p)
        {
            double result = 0;

            int i = 0;

            foreach (SDF item in Items)
            {
                double d = item.Evaluate(p);
                if (i == 0 || d > result)
                {
                    result = d;
                }
                i++;
            }
            return result;
        }

        Box SDF.BoundingBox()
        {
            Box result = new Box();
            int i = 0;

            foreach (SDF item in Items)
            {
                Box box = item.BoundingBox();
                if (i == 0)
                {
                    result = box;
                }
                else
                {
                    result = result.Extend(box);
                }
                i++;
            }
            return result;
        }
    }

    class RepeatSDF : SDF
    {
        SDF SDF;
        V Step;

        RepeatSDF(SDF sdf, V step)
        {
            SDF = sdf;
            Step = step;
        }

        internal static SDF NewRepeaterSDF(SDF sdf, V step)
        {
            return new RepeatSDF(sdf, step);
        }

        double SDF.Evaluate(V p)
        {
            V q = p.Mod(Step).Sub(Step.DivScalar(2));
            return SDF.Evaluate(q);
        }

        Box SDF.BoundingBox()
        {
            return new Box();
        }
    }
}
