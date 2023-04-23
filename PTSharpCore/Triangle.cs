using System.Numerics;

namespace PTSharpCore
{
    public class Triangle : IShape
    {
        internal Material Material;
        public Vector V1, V2, V3;
        public Vector N1, N2, N3;
        public Vector T1, T2, T3;

        internal Triangle() { }

        internal Triangle(Vector v1, Vector v2, Vector v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        internal Triangle(Vector v1, Vector v2, Vector v3, Material Material)
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

        internal static Triangle NewTriangle(Vector v1, Vector v2, Vector v3, Vector n1, Vector n2, Vector n3, Vector t1, Vector t2, Vector t3, Material material)
        {
            Triangle t = new Triangle();
            t.V1 = v1;
            t.V2 = v2;
            t.V3 = v3;
            t.N1 = n1;
            t.N2 = n2;
            t.N3 = n3;
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

        Hit IShape.Intersect(Ray r)
        {
            var e1x = V2.X - V1.X;
            var e1y = V2.Y - V1.Y;
            var e1z = V2.Z - V1.Z;
            var e2x = V3.X - V1.X;
            var e2y = V3.Y - V1.Y;
            var e2z = V3.Z - V1.Z;
            var px = r.Direction.Y * e2z - r.Direction.Z * e2y;
            var py = r.Direction.Z * e2x - r.Direction.X * e2z;
            var pz = r.Direction.X * e2y - r.Direction.Y * e2x;
            var det = e1x * px + e1y * py + e1z * pz;

            if (det > -Util.EPS && det < Util.EPS)
            {
                return Hit.NoHit;
            }

            var inv = 1 / det;
            var tx = r.Origin.X - V1.X;
            var ty = r.Origin.Y - V1.Y;
            var tz = r.Origin.Z - V1.Z;
            var u = (tx * px + ty * py + tz * pz) * inv;

            if (u < 0 || u > 1)
            {
                return Hit.NoHit;
            }

            var qx = ty * e1z - tz * e1y;
            var qy = tz * e1x - tx * e1z;
            var qz = tx * e1y - ty * e1x;
            var v = (r.Direction.X * qx + r.Direction.Y * qy + r.Direction.Z * qz) * inv;

            if ((v < 0) || ((u + v) > 1))
            {
                return Hit.NoHit;

            }

            var d = (e2x * qx + e2y * qy + e2z * qz) * inv;

            if (d < Util.EPS)
            {
                return Hit.NoHit;
            }

            return new Hit(this, d, null);
        }

        Vector IShape.UV(Vector p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = new Vector();
            n = n.Add(T1.MulScalar(u));
            n = n.Add(T2.MulScalar(v));
            n = n.Add(T3.MulScalar(w));
            return new Vector(n.X, n.Y, 0);
        }

        Vector SetUV(Vector2 p)
        {
            return new Vector(p.X, p.Y, 0);
        }

        Material IShape.MaterialAt(Vector v) => Material;

        Vector IShape.NormalAt(Vector p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = new Vector();
            n = n.Add(N1.MulScalar(u));
            n = n.Add(N2.MulScalar(v));
            n = n.Add(N3.MulScalar(w));
            n = n.Normalize();

            if (Material.NormalTexture != null)
            {
                var b = new Vector();
                b = b.Add(T1.MulScalar(u));
                b = b.Add(T2.MulScalar(v));
                b = b.Add(T3.MulScalar(w));
                var ns = Material.NormalTexture.NormalSample(b.X, b.Y);
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

            if (Material.BumpTexture != null)
            {
                var b = new Vector();
                b = b.Add(T1.MulScalar(u));
                b = b.Add(T2.MulScalar(v));
                b = b.Add(T3.MulScalar(w));
                var bump = Material.BumpTexture.BumpSample(b.X, b.Y);
                var dv1 = V2.Sub(V1);
                var dv2 = V3.Sub(V1);
                var dt1 = T2.Sub(T1);
                var dt2 = T3.Sub(T1);
                var tangent = dv1.MulScalar(dt2.Y).Sub(dv2.MulScalar(dt1.Y)).Normalize();
                var bitangent = dv2.MulScalar(dt1.X).Sub(dv1.MulScalar(dt2.X)).Normalize();
                n = n.Add(tangent.MulScalar(bump.X * Material.BumpMultiplier));
                n = n.Add(bitangent.MulScalar(bump.Y * Material.BumpMultiplier));
            }
            n = n.Normalize();
            return n;
        }

        double Area()
        {
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
