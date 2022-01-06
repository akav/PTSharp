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
        double Evaluate(IVector<double> p);
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
            double epsilon = 0.00001;
            double start = 0.0001;
            double jumpSize = 0.001;
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

        IVector<double> IShape.UV(IVector<double> uv)
        {
            return new IVector<double>();
        }

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            double e = 0.0001;
            (var x, var y, var z) = (p.dv[0], p.dv[1], p.dv[2]);

            var n = new IVector<double>(new double[] {
                Evaluate(new IVector<double>(new double[] {x - e, y, z,0 })) - Evaluate(new IVector<double>(new double[] {x + e, y, z,0 })),
                Evaluate(new IVector<double>(new double[] {x, y - e, z,0 })) - Evaluate(new IVector<double>(new double[] {x, y + e, z,0 })),
                Evaluate(new IVector<double>(new double[] {x, y, z - e,0 })) - Evaluate(new IVector<double>(new double[] {x, y, z + e,0 })), 0 });
            return n.Normalize();
        }

        Material IShape.MaterialAt(IVector<double> v)
        {
            return Material;
        }

        public Box BoundingBox()
        {
            return SDF.BoundingBox();
        }

        public double Evaluate(IVector<double> p)
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

        double SDF.Evaluate(IVector<double> p)
        {
            return p.LengthN(Exponent) - Radius;
        }

        Box SDF.BoundingBox()
        {
            double r = Radius;
            return new Box(new IVector<double>(new double[] { -r, -r, -r, 0 }), new IVector<double>(new double[] { r, r, r, 0 }));
        }
    }

    internal class CubeSDF : SDF
    {
        IVector<double> Size;

        CubeSDF(IVector<double> size)
        {
            Size = size;
        }

        internal static SDF NewCubeSDF(IVector<double> size)
        {
            return new CubeSDF(size);
        }

        double SDF.Evaluate(IVector<double> p)
        {
            double x = p.dv[0];
            double y = p.dv[1];
            double z = p.dv[2];

            if (x < 0)
                x = -x;
            if (y < 0)
                y = -y;
            if (z < 0)
                z = -z;

            x -= Size.dv[0] / 2;
            y -= Size.dv[1] / 2;
            z -= Size.dv[2] / 2;

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
            (var x, var y, var z) = (Size.dv[0] / 2, Size.dv[1] / 2, Size.dv[2] / 2);
            return new Box(new IVector<double>(new double[] { -x, -y, -z, 0 }), new IVector<double>(new double[] { x, y, z, 0 }));
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
            return new Box(new IVector<double>(new double[] { -r, -h, -r, 0 }), new IVector<double>(new double[] { r, h, r, 0 }));
        }
        Box SDF.BoundingBox()
        {
            double r = Radius;
            double h = Height / 2;
            return new Box(new IVector<double>(new double[] { -r, -h, -r, 0 }), new IVector<double>(new double[] { r, h, r, 0 }));
        }

        double SDF.Evaluate(IVector<double> p)
        {
            double x = Math.Sqrt(p.dv[0] * p.dv[0] + p.dv[2] * p.dv[2]);
            double y = p.dv[1];

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
        IVector<double> A, B;
        double Radius;
        double Exponent;

        CapsuleSDF(IVector<double> A, IVector<double> B, double Radius, double Exponent)
        {
            this.A = A;
            this.B = B;
            this.Radius = Radius;
            this.Exponent = Exponent;
        }
        internal static SDF NewCapsuleSDF(IVector<double> a, IVector<double> b, double radius)
        {
            return new CapsuleSDF(a, b, radius, 2);
        }

        double SDF.Evaluate(IVector<double> p)
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

        double SDF.Evaluate(IVector<double> p)
        {
            IVector<double> q = new IVector<double>(new double[] { new IVector<double>(new double[] { p.dv[0], p.dv[1], 0, 0 }).LengthN(MajorExponent) - MajorRadius, p.dv[2], 0, 0 });
            return q.LengthN(MinorExponent) - MinRadius;
        }

        Box SDF.BoundingBox()
        {
            double a = MinRadius;
            double b = MinRadius + MajorRadius;
            return new Box(new IVector<double>(new double[] { -b, -b, a, 0 }), new IVector<double>(new double[] { b, b, a, 0 }));
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

        double SDF.Evaluate(IVector<double> p)
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

        double SDF.Evaluate(IVector<double> p)
        {
            return SDF.Evaluate(p.DivScalar(Factor)) * Factor;
        }

        Box SDF.BoundingBox()
        {
            double f = Factor;
            Matrix m = new Matrix().Scale(new IVector<double>(new double[] { f, f, f, 0 }));
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

        double SDF.Evaluate(IVector<double> p)
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


        double SDF.Evaluate(IVector<double> p)
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

        double SDF.Evaluate(IVector<double> p)
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
        IVector<double> Step;

        RepeatSDF(SDF sdf, IVector<double> step)
        {
            SDF = sdf;
            Step = step;
        }

        internal static SDF NewRepeaterSDF(SDF sdf, IVector<double> step)
        {
            return new RepeatSDF(sdf, step);
        }

        double SDF.Evaluate(IVector<double> p)
        {
            IVector<double> q = p.Mod(Step).Sub(Step.DivScalar(2));
            return SDF.Evaluate(q);
        }

        Box SDF.BoundingBox()
        {
            return new Box();
        }
    }
}
