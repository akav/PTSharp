using MathNet.Numerics.Optimization;
using System;
using System.Threading;

namespace PTSharpCore
{
    class Cube : IShape
    {
        internal V Min;
        internal V Max;
        internal Material Material;
        internal Box Box;

        Cube(V min, V max, Material material, Box box)
        {
            Min = min;
            Max = max;
            Material = material;
            Box = box;
        }

        internal static Cube NewCube(V min, V max, Material material)
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
            var t0 = Math.Max(Math.Max(n.v.X, n.v.Y), n.v.Z);
            var t1 = Math.Min(Math.Min(f.v.X, f.v.Y), f.v.Z);

            if (t0 > 0 && t0 < t1)
            {
                return new Hit(this, t0, null);
            }
            return Hit.NoHit;
        }

        V IShape.UV(V p)
        {
            p = p.Sub(Min).Div(Max.Sub(Min));
            return new V(p.v.X, p.v.Z, 0);
        }

        Material IShape.MaterialAt(V p) => Material;

        V IShape.NormalAt(V p)
        {
            return p switch
            {
                V when p.v.X < Min.v.X + Util.EPS => new V(-1, 0, 0),
                V when p.v.X > Max.v.X - Util.EPS => new V(1, 0, 0),
                V when p.v.Y < Min.v.Y + Util.EPS => new V(0, -1, 0),
                V when p.v.Y > Max.v.Y - Util.EPS => new V(0, 1, 0),
                V when p.v.Z < Min.v.Z + Util.EPS => new V(0, 0, -1),
                V when p.v.Z > Max.v.Z - Util.EPS => new V(0, 0, 1),
                _ => new V(0, 1, 0),
            };
        }
        
        Mesh CubeMesh()
        {
            var a = Min;
            var b = Max;
            var z = new V();
            var m = Material;
            var v000 = new V(a.v.X, a.v.Y, a.v.Z);
            var v001 = new V(a.v.X, a.v.Y, b.v.Z);
            var v010 = new V(a.v.X, b.v.Y, a.v.Z);
            var v011 = new V(a.v.X, b.v.Y, b.v.Z);
            var v100 = new V(b.v.X, a.v.Y, a.v.Z);
            var v101 = new V(b.v.X, a.v.Y, b.v.Z);
            var v110 = new V(b.v.X, b.v.Y, a.v.Z);
            var v111 = new V(b.v.X, b.v.Y, b.v.Z);
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
