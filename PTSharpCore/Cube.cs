using MathNet.Numerics.Optimization;
using System;
using System.Threading;

namespace PTSharpCore
{
    class Cube : IShape
    {
        internal IVector<double> Min;
        internal IVector<double> Max;
        internal Material Material;
        internal Box Box;

        Cube(IVector<double> min, IVector<double> max, Material material, Box box)
        {
            Min = min;
            Max = max;
            Material = material;
            Box = box;
        }

        internal static Cube NewCube(IVector<double> min, IVector<double> max, Material material)
        {
            Box box = new Box(min, max);
            return new Cube(min, max, material, box);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox() => Box;

        Hit IShape.Intersect(Ray r)
        {
            var n = Min.Sub(r.Origin).Div(r.Direction);
            var f = Max.Sub(r.Origin).Div(r.Direction);
            (n, f) = (n.Min(f), n.Max(f));
            var t0 = Math.Max(Math.Max(n.dv[0], n.dv[1]), n.dv[2]);
            var t1 = Math.Min(Math.Min(f.dv[0], f.dv[1]), f.dv[2]);

            if (t0 > 0 && t0 < t1) {
                return new Hit(this, t0, null);
            }
            return Hit.NoHit;
        }

        IVector<double> IShape.UV(IVector<double> p)
        {
            p = p.Sub(Min).Div(Max.Sub(Min));
            return new IVector<double>(new double[] { p.dv[0], p.dv[2], 0, 0 });
        }

        Material IShape.MaterialAt(IVector<double> p) => Material;

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            return p switch
            {
                IVector<double> when p.dv[0] < Min.dv[0] + Util.EPS => new IVector<double>(new double[] { -1, 0, 0, 0 }),
                IVector<double> when p.dv[0] > Max.dv[0] - Util.EPS => new IVector<double>(new double[] { 1, 0, 0, 0  }),
                IVector<double> when p.dv[1] < Min.dv[1] + Util.EPS => new IVector<double>(new double[] { 0, -1, 0, 0 }),
                IVector<double> when p.dv[1] > Max.dv[1] - Util.EPS => new IVector<double>(new double[] { 0, 1, 0, 0  }),
                IVector<double> when p.dv[2] < Min.dv[2] + Util.EPS => new IVector<double>(new double[] { 0, 0, -1, 0 }),
                IVector<double> when p.dv[2] > Max.dv[2] - Util.EPS => new IVector<double>(new double[] { 0, 0, 1, 0 }),
                _ => new IVector<double>(new double[] { 0, 1, 0, 0 }),
            };
        }
        
        Mesh CubeMesh()
        {
            var a = Min;
            var b = Max;
            var z = new IVector<double>();
            var m = Material;
            var v000 = new IVector<double>(new double[] { a.dv[0], a.dv[1], a.dv[2], 0 });
            var v001 = new IVector<double>(new double[] { a.dv[0], a.dv[1], b.dv[2], 0 });
            var v010 = new IVector<double>(new double[] { a.dv[0], b.dv[1], a.dv[2], 0 });
            var v011 = new IVector<double>(new double[] { a.dv[0], b.dv[1], b.dv[2], 0 });
            var v100 = new IVector<double>(new double[] { b.dv[0], a.dv[1], a.dv[2], 0 });
            var v101 = new IVector<double>(new double[] { b.dv[0], a.dv[1], b.dv[2], 0 });
            var v110 = new IVector<double>(new double[] { b.dv[0], b.dv[1], a.dv[2], 0 });
            var v111 = new IVector<double>(new double[] { b.dv[0], b.dv[1], b.dv[2], 0 });
            Triangle[] triangles = {
                Triangle.NewTriangle(v000, v100, v110, z, z, z, m),
                Triangle.NewTriangle(v000, v110, v010, z, z, z, m),
                Triangle.NewTriangle(v001, v101, v111, z, z, z, m),
                Triangle.NewTriangle(v001, v111, v011, z, z, z, m),
                Triangle.NewTriangle(v000, v100, v101, z, z, z, m),
                Triangle.NewTriangle(v000, v101, v001, z, z, z, m),
                Triangle.NewTriangle(v010, v110, v111, z, z, z, m),
                Triangle.NewTriangle(v010, v111, v011, z, z, z, m),
                Triangle.NewTriangle(v000, v010, v011, z, z, z, m),
                Triangle.NewTriangle(v000, v011, v001, z, z, z, m),
                Triangle.NewTriangle(v100, v110, v111, z, z, z, m),
                Triangle.NewTriangle(v100, v111, v101, z, z, z, m)
            };
            return Mesh.NewMesh(triangles);
        }
    }
}
