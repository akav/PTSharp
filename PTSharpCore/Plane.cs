using System;

namespace PTSharpCore
{
    class Plane : IShape
    {
        V Point;
        V Normal;
        Material Material;
        Box box;

        Plane() { }

        Plane(V point, V normal, Material mat)
        {
            Point = point;
            Normal = normal;
            Material = mat;
            box = new Box(new V(-Util.INF, -Util.INF, -Util.INF), new V(Util.INF, Util.INF, Util.INF));
        }

        internal static Plane NewPlane(V point, V normal, Material material)
        {
            return new Plane(point, normal.Normalize(), material);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox()
        {
            return new Box(new V(-Util.INF, -Util.INF, -Util.INF), new V(Util.INF, Util.INF, Util.INF));
        }

        Hit IShape.Intersect(Ray ray)
        {
            float d = Normal.Dot(ray.Direction);
            if (MathF.Abs(d) < Util.EPS)
            {
                return Hit.NoHit;
            }
            V a = Point.Sub(ray.Origin);
            float t = a.Dot(Normal) / d;
            if (t < Util.EPS)
            {
                return Hit.NoHit;
            }
            return new Hit(this, t, null);
        }

        V IShape.UV(V a)
        {
            return new V();
        }

        public Material MaterialAt(V v)
        {
            return Material;
        }
        V IShape.NormalAt(V a)
        {
            return Normal;
        }
    }
}
