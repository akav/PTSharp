using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Triangle : IShape
    {
        public Material Material;
        public Vector V1, V2, V3;
        public Vector N1, N2, N3;
        public Vector T1, T2, T3;
        
        public Triangle() { }

        internal Triangle(Vector v1, Vector v2, Vector v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        internal Triangle (Vector v1, Vector v2, Vector v3, Material Material)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            this.Material = Material;
        }

        internal static Triangle NewTriangle(Vector v1, Vector v2, Vector v3, Vector t1, Vector t2, Vector t3, Material material)
        {
            Triangle t = new Triangle();
            t.V1 = v1;
            t.V2 = v2;
            t.V3 = v3;
            t.T1 = t1;
            t.T2 = t2;
            t.T3 = t3;
            t.Material = material;
            t.FixNormals();
            return t;
        }

        (Vector, Vector, Vector) Vertices()
        {
            return (V1, V2, V3);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox()
        {
            var min = V1.Min(V2).Min(V3);
            var max = V1.Max(V2).Max(V3);
            return new Box(min, max);
        }

        internal Box BoundingBox()
        {
            var min = V1.Min(V2).Min(V3);
            var max = V1.Max(V2).Max(V3);
            return new Box(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Hit IShape.Intersect(Ray r)
        {
            var e1 = V2.Sub(V1);
            var e2 = V3.Sub(V1);
            var h = r.Direction.Cross(e2);
            var det = e1.Dot(h);

            if (det > -Util.EPS && det < Util.EPS)
                return Hit.NoHit;

            var invDet = 1.0 / det;
            var s = r.Origin.Sub(V1);
            var u = s.Dot(h) * invDet;

            if (u < 0 || u > 1)
                return Hit.NoHit;

            var q = s.Cross(e1);
            var v = r.Direction.Dot(q) * invDet;

            if (v < 0 || (u + v) > 1)
                return Hit.NoHit;

            var t = e2.Dot(q) * invDet;

            if (t < Util.EPS)
                return Hit.NoHit;

            return new Hit(this, t, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        Vector IShape.UVector(Vector p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = new Vector();
            n = n.Add(T1.MulScalar(u));
            n = n.Add(T2.MulScalar(v));
            n = n.Add(T3.MulScalar(w));
            return new Vector(n.X, n.Y, 0);
        }

        Material IShape.MaterialAt(Vector v) => Material;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        Vector IShape.NormalAt(Vector p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = N1.MulScalar(u).Add(N2.MulScalar(v)).Add(N3.MulScalar(w));

            if (Material.NormalTexture != null)
            {
                var b = T1.MulScalar(u).Add(T2.MulScalar(v)).Add(T3.MulScalar(w));
                var ns = Material.NormalTexture.NormalSample(b.X, b.Y);

                if (ns != Vector.Zero)
                {
                    var dv1 = V2.Sub(V1);
                    var dv2 = V3.Sub(V1);
                    var dt1 = T2.Sub(T1);
                    var dt2 = T3.Sub(T1);
                    var T = dv1.MulScalar(dt2.Y).Sub(dv2.MulScalar(dt1.Y)).Normalize();
                    var B = dv2.MulScalar(dt1.X).Sub(dv1.MulScalar(dt2.X)).Normalize();
                    var N = T.Cross(B);

                    var matrix = new Matrix(T.X, B.X, N.X, 0,
                                            T.Y, B.Y, N.Y, 0,
                                            T.Z, B.Z, N.Z, 0,
                                            0, 0, 0, 1);
                    n = matrix.MulDirection(ns);
                }
            }

            if (Material.BumpTexture != null)
            {
                var b = T1.MulScalar(u).Add(T2.MulScalar(v)).Add(T3.MulScalar(w));
                var bump = Material.BumpTexture.BumpSample(b.X, b.Y);

                if (bump != Vector.Zero)
                {
                    var dv1 = V2.Sub(V1);
                    var dv2 = V3.Sub(V1);
                    var dt1 = T2.Sub(T1);
                    var dt2 = T3.Sub(T1);
                    var tangent = dv1.MulScalar(dt2.Y).Sub(dv2.MulScalar(dt1.Y)).Normalize();
                    var bitangent = dv2.MulScalar(dt1.X).Sub(dv1.MulScalar(dt2.X)).Normalize();
                    n = n.Add(tangent.MulScalar(bump.X * Material.BumpMultiplier));
                    n = n.Add(bitangent.MulScalar(bump.Y * Material.BumpMultiplier));
                }
            }

            return n.Normalize();
        }


        double Area() {
            var e1 = V2.Sub(V1);
            var e2 = V3.Sub(V1);
            var n = e1.Cross(e2);
            return n.Length() / 2;
        }

        Vector Normal()
        {
            var e1 = V2.Sub(V1);
            var e2 = V3.Sub(V1);
            return e1.Cross(e2).Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        (double, double, double) Barycentric(Vector p)
        {
            var v0 = V2.Sub(V1);
            var v1 = V3.Sub(V1);
            var v2 = p.Sub(V1);
            var d00 = v0.Dot(v0);
            var d01 = v0.Dot(v1);
            var d11 = v1.Dot(v1);
            var d20 = v2.Dot(v0);
            var d21 = v2.Dot(v1);
            var d = d00 * d11 - d01 * d01;
            var v = (d11 * d20 - d01 * d21) / d;
            var w = (d00 * d21 - d01 * d20) / d;
            var u = 1 - v - w;
            return (u, v, w);
        }
        public void FixNormals()
        {
            var n = Normal();
            var zero = new Vector();
            
            if (N1.Equals(zero))
                N1 = n;

            if (N2.Equals(zero))
                N2 = n;

            if (N3.Equals(zero))
                N3 = n;
        }
    }
}
