using System;

namespace PTSharpCore
{
    class Plane : IShape
    {
        Vector Point;
        Vector Normal;
        Material Material;
        Box box;

        Plane() { }

        Plane(Vector point, Vector normal, Material mat)
        {
            Point = point;
            Normal = normal;
            Material = mat;
            box = new Box(new Vector(-Util.INF, -Util.INF, -Util.INF), new Vector(Util.INF, Util.INF, Util.INF));
        }

        internal static Plane NewPlane(Vector point, Vector normal, Material material)
        {
            return new Plane(point, normal.Normalize(), material);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox()
        {
            return new Box(new Vector(-Util.INF, -Util.INF, -Util.INF), new Vector(Util.INF, Util.INF, Util.INF));
        }

        Hit IShape.Intersect(Ray ray)
        {
            double d = Normal.Dot(ray.Direction);
            if (Math.Abs(d) < Util.EPS)
            {
                return Hit.NoHit;
            }
            Vector a = Point.Sub(ray.Origin);
            double t = a.Dot(Normal) / d;
            if (t < Util.EPS)
            {
                return Hit.NoHit;
            }
            return new Hit(this, t, null);
        }

        Vector IShape.UV(Vector a)
        {
            return new Vector();
        }

        public Material MaterialAt(Vector v)
        {
            return Material;
        }
        Vector IShape.NormalAt(Vector a)
        {
            return Normal;
        }
    }
}
