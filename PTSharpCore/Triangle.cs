using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{   
    class Triangle : IShape
    {
        internal Material Material;
        public IVector<double> V1, V2, V3;
        public IVector<double> N1, N2, N3;
        public IVector<double> T1, T2, T3;

        internal Triangle() { }

        internal Triangle(IVector<double> v1, IVector<double> v2, IVector<double> v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        internal Triangle (IVector<double> v1, IVector<double> v2, IVector<double> v3, Material Material)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            this.Material = Material;
        }

        internal static Triangle NewTriangle(IVector<double> v1, IVector<double> v2, IVector<double> v3, IVector<double> t1, IVector<double> t2, IVector<double> t3, Material material)
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

        (IVector<double>, IVector<double>, IVector<double>) Vertices()
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
            var e1x = V2.dv[0] - V1.dv[0];
            var e1y = V2.dv[1] - V1.dv[1];
            var e1z = V2.dv[2] - V1.dv[2];
            var e2x = V3.dv[0] - V1.dv[0];
            var e2y = V3.dv[1] - V1.dv[1];
            var e2z = V3.dv[2] - V1.dv[2];
            var px = r.Direction.dv[1] * e2z - r.Direction.dv[2] * e2y;
            var py = r.Direction.dv[2] * e2x - r.Direction.dv[0] * e2z;
            var pz = r.Direction.dv[0] * e2y - r.Direction.dv[1] * e2x;
            var det = e1x * px + e1y * py + e1z * pz;

            if (det > -Util.EPS && det < Util.EPS) 
            {
                return Hit.NoHit;
            }

            var inv = 1 / det;
            var tx = r.Origin.dv[0] - V1.dv[0];
            var ty = r.Origin.dv[1] - V1.dv[1];
            var tz = r.Origin.dv[2] - V1.dv[2];
            var u = (tx * px + ty * py + tz * pz) * inv;

            if(u < 0 || u > 1)
            {
                return Hit.NoHit;
            }

            var qx = ty * e1z - tz * e1y;
            var qy = tz * e1x - tx * e1z;
            var qz = tx * e1y - ty * e1x;
            var v = (r.Direction.dv[0] * qx + r.Direction.dv[1] * qy + r.Direction.dv[2] * qz) * inv;

            if((v < 0) || ((u + v) > 1))
            {
                return Hit.NoHit;

            }

            var d = (e2x * qx + e2y * qy + e2z * qz) * inv;

            if(d < Util.EPS) {
                return Hit.NoHit;
            }

            return new Hit(this, d, null);
        }

        IVector<double> IShape.UV(IVector<double> p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = new IVector<double>();
            n = n.Add(T1.MulScalar(u));
            n = n.Add(T2.MulScalar(v));
            n = n.Add(T3.MulScalar(w));
            return new IVector<double>(new double[] { n.dv[0], n.dv[1], 0, 0 });
        }

        Material IShape.MaterialAt(IVector<double> v) => Material;

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            (var u, var v, var w) = Barycentric(p);
            var n = new IVector<double>();
            n = n.Add(N1.MulScalar(u));
            n = n.Add(N2.MulScalar(v));
            n = n.Add(N3.MulScalar(w));
            n = n.Normalize();

            if(Material.NormalTexture != null)
            {
                var b = new IVector<double>();
                b = b.Add(T1.MulScalar(u));
                b = b.Add(T2.MulScalar(v));
                b = b.Add(T3.MulScalar(w));
                var ns = Material.NormalTexture.NormalSample(b.dv[0], b.dv[1]);
                var dv1 = V2.Sub(V1);
                var dv2 = V3.Sub(V1);
                var dt1 = T2.Sub(T1);
                var dt2 = T3.Sub(T1);
                var T = dv1.MulScalar(dt2.dv[1]).Sub(dv2.MulScalar(dt1.dv[1])).Normalize();
                var B = dv2.MulScalar(dt1.dv[0]).Sub(dv1.MulScalar(dt2.dv[0])).Normalize();
                var N = T.Cross(B);

                var matrix = new Matrix(T.dv[0], B.dv[0], N.dv[0], 0,
                                        T.dv[1], B.dv[1], N.dv[1], 0,
                                        T.dv[2], B.dv[2], N.dv[2], 0,
                                        0, 0, 0, 1);
                n = matrix.MulDirection(ns);
            }

            if(Material.BumpTexture != null)
            {
                var b = new IVector<double>();
                b = b.Add(T1.MulScalar(u));
                b = b.Add(T2.MulScalar(v));
                b = b.Add(T3.MulScalar(w));
                var bump = Material.BumpTexture.BumpSample(b.dv[0], b.dv[1]);
                var dv1 = V2.Sub(V1);
                var dv2 = V3.Sub(V1);
                var dt1 = T2.Sub(T1);
                var dt2 = T3.Sub(T1);
                var tangent = dv1.MulScalar(dt2.dv[1]).Sub(dv2.MulScalar(dt1.dv[1])).Normalize();
                var bitangent = dv2.MulScalar(dt1.dv[0]).Sub(dv1.MulScalar(dt2.dv[0])).Normalize();
                n = n.Add(tangent.MulScalar(bump.dv[0] * Material.BumpMultiplier));
                n = n.Add(bitangent.MulScalar(bump.dv[1] * Material.BumpMultiplier));
            }
            n = n.Normalize();
            return n;
        }
                
        double Area() {
            var e1 = V2.Sub(V1);
            var e2 = V3.Sub(V1);
            var n = e1.Cross(e2);
            return n.Length() / 2;
        }
        
        IVector<double> Normal()
        {
            var e1 = V2.Sub(V1);
            var e2 = V3.Sub(V1);
            return e1.Cross(e2).Normalize();
        }
        (double, double, double) Barycentric(IVector<double> p)
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
            var zero = new IVector<double>();
            
            if (N1.Equals(zero))
                N1 = n;

            if (N2.Equals(zero))
                N2 = n;

            if (N3.Equals(zero))
                N3 = n;
        }
    }
}
