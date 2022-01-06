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
        float Evaluate(V p);
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
            float epsilon = 0.00001F;
            float start = 0.0001F;
            float jumpSize = 0.001F;
            Box box = BoundingBox();
            (float t1, float t2) = box.Intersect(ray);
            if (t2 < t1 || t2 < 0)
            {
                return Hit.NoHit;
            }

            float t = MathF.Max(start, t1);
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
            float e = 0.0001F;
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

        public float Evaluate(V p)
        {
            return SDF.Evaluate(p);
        }
    }

    internal class SphereSDF : SDF
    {
        float Radius;
        float Exponent;

        SphereSDF(float Radius, float Exponent)
        {
            this.Radius = Radius;
            this.Exponent = Exponent;
        }
        internal static SDF NewSphereSDF(float radius)
        {
            return new SphereSDF(radius, 2);
        }

        float SDF.Evaluate(V p)
        {
            return p.LengthN(Exponent) - Radius;
        }

        Box SDF.BoundingBox()
        {
            float r = Radius;
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

        float SDF.Evaluate(V p)
        {
            float x = p.v.X;
            float y = p.v.Y;
            float z = p.v.Z;

            if (x < 0)
                x = -x;
            if (y < 0)
                y = -y;
            if (z < 0)
                z = -z;

            x -= Size.v.X / 2;
            y -= Size.v.Y / 2;
            z -= Size.v.Z / 2;

            float a = x;
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
            float b = MathF.Sqrt(x * x + y * y + z * z);
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
        float Radius;
        float Height;

        CylinderSDF(float Radius, float Height)
        {
            this.Radius = Radius;
            this.Height = Height;
        }

        internal static SDF NewCylinderSDF(float radius, float height)
        {
            return new CylinderSDF(radius, height);
        }

        internal Box BoundingBox()
        {
            float r = Radius;
            float h = Height / 2;
            return new Box(new V(-r, -h, -r), new V(r, h, r));
        }
        Box SDF.BoundingBox()
        {
            float r = Radius;
            float h = Height / 2;
            return new Box(new V(-r, -h, -r), new V(r, h, r));
        }

        float SDF.Evaluate(V p)
        {
            float x = MathF.Sqrt(p.v.X * p.v.X + p.v.Z * p.v.Z);
            float y = p.v.Y;

            if (x < 0)
                x = -x;
            if (y < 0)
                y = -y;
            x -= Radius;
            y -= Height / 2;

            float a = x;

            if (y > a)
                a = y;
            if (a > 0)
                a = 0;
            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;

            float b = MathF.Sqrt(x * x + y * y);
            return a + b;
        }
    }

    class CapsuleSDF : SDF
    {
        V A, B;
        float Radius;
        float Exponent;

        CapsuleSDF(V A, V B, float Radius, float Exponent)
        {
            this.A = A;
            this.B = B;
            this.Radius = Radius;
            this.Exponent = Exponent;
        }
        internal static SDF NewCapsuleSDF(V a, V b, float radius)
        {
            return new CapsuleSDF(a, b, radius, 2);
        }

        float SDF.Evaluate(V p)
        {
            var pa = p.Sub(A);
            var ba = B.Sub(A);
            var h = MathF.Max(0, MathF.Min(1, pa.Dot(ba) / ba.Dot(ba)));
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
        float MajorRadius;
        float MinRadius;
        float MajorExponent;
        float MinorExponent;

        TorusSDF(float MajorRadius, float MinRadius, float MajorExponent, float MinorExponent)
        {
            this.MajorRadius = MajorRadius;
            this.MinRadius = MinRadius;
            this.MajorExponent = MajorExponent;
            this.MinorExponent = MinorExponent;
        }

        internal static SDF NewTorusSDF(float major, float minor)
        {
            return new TorusSDF(major, minor, 2, 2);
        }

        float SDF.Evaluate(V p)
        {
            V q = new V(new V(p.v.X, p.v.Y, 0).LengthN(MajorExponent) - MajorRadius, p.v.Z, 0);
            return q.LengthN(MinorExponent) - MinRadius;
        }

        Box SDF.BoundingBox()
        {
            float a = MinRadius;
            float b = MinRadius + MajorRadius;
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

        float SDF.Evaluate(V p)
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
        float Factor;

        ScaleSDF(SDF sdf, float Factor)
        {
            SDF = sdf;
            this.Factor = Factor;
        }

        internal static SDF NewScaleSDF(SDF sdf, float factor)
        {
            return new ScaleSDF(sdf, factor);
        }

        float SDF.Evaluate(V p)
        {
            return SDF.Evaluate(p.DivScalar(Factor)) * Factor;
        }

        Box SDF.BoundingBox()
        {
            float f = Factor;
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

        float SDF.Evaluate(V p)
        {
            float result = 0;
            int i = 0;
            foreach (SDF item in Items)
            {
                float d = item.Evaluate(p);
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


        float SDF.Evaluate(V p)
        {
            float result = 0;
            int i = 0;

            foreach (SDF item in Items)
            {
                float d = item.Evaluate(p);
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

        float SDF.Evaluate(V p)
        {
            float result = 0;

            int i = 0;

            foreach (SDF item in Items)
            {
                float d = item.Evaluate(p);
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

        float SDF.Evaluate(V p)
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
