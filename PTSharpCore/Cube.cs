using MathNet.Numerics.Optimization;
using System;
using System.Threading;

namespace PTSharpCore
{
    class Cube : IShape
    {
        internal Vector Min;
        internal Vector Max;
        internal Material Material;
        internal Box Box;

        Cube(Vector min, Vector max, Material material, Box box)
        {
            Min = min;
            Max = max;
            Material = material;
            Box = box;
        }

        internal static Cube NewCube(Vector min, Vector max, Material material)
        {
            Box box = new Box(min, max);
            return new Cube(min, max, material, box);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox() => Box;

        Hit IShape.Intersect(Ray r)
        {
            double tmin = (Min.x - r.Origin.x) / r.Direction.x;
            double tmax = (Max.x - r.Origin.x) / r.Direction.x;

            if (tmin > tmax)
                (tmin, tmax) = (tmax, tmin);

            double tymin = (Min.y - r.Origin.y) / r.Direction.y;
            double tymax = (Max.y - r.Origin.y) / r.Direction.y;

            if (tymin > tymax) 
                (tymin, tymax) = (tymax, tymin);

            if ((tmin > tymax) || (tymin > tmax))
                return Hit.NoHit;

            if (tymin > tmin)
                tmin = tymin;

            if (tymax < tmax)
                tmax = tymax;

            double tzmin = (Min.z - r.Origin.z) / r.Direction.z;
            double tzmax = (Max.z - r.Origin.z) / r.Direction.z;

            if (tzmin > tzmax) 
                (tzmin, tzmax) = (tzmax, tzmin);

            if ((tmin > tzmax) || (tzmin > tmax))
                return Hit.NoHit;

            if (tzmin > tmin)
                tmin = tzmin;

            if (tzmax < tmax)
                tmax = tzmax;

            return tmin > 0 && tmin < tmax ? new Hit(this, tmin, null) : Hit.NoHit;
        }

        Vector IShape.UV(Vector p)
        {
            p = p.Sub(Min).Div(Max.Sub(Min));
            return new Vector(p.x, p.z, 0);
        }

        Material IShape.MaterialAt(Vector p) => Material;

        Vector IShape.NormalAt(Vector p)
        {
            return p switch
            {
                Vector when p.x < Min.x + Util.EPS => new Vector(-1, 0, 0),
                Vector when p.x > Max.x - Util.EPS => new Vector(1, 0, 0),
                Vector when p.y < Min.y + Util.EPS => new Vector(0, -1, 0),
                Vector when p.y > Max.y - Util.EPS => new Vector(0, 1, 0),
                Vector when p.z < Min.z + Util.EPS => new Vector(0, 0, -1),
                Vector when p.z > Max.z - Util.EPS => new Vector(0, 0, 1),
                _ => new Vector(0, 1, 0),
            };
        }
        
        Mesh CubeMesh()
        {
            var a = Min;
            var b = Max;
            var z = new Vector();
            var m = Material;
            var v000 = new Vector(a.x, a.y, a.z);
            var v001 = new Vector(a.x, a.y, b.z);
            var v010 = new Vector(a.x, b.y, a.z);
            var v011 = new Vector(a.x, b.y, b.z);
            var v100 = new Vector(b.x, a.y, a.z);
            var v101 = new Vector(b.x, a.y, b.z);
            var v110 = new Vector(b.x, b.y, a.z);
            var v111 = new Vector(b.x, b.y, b.z);
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
