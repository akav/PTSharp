using System;

namespace PTSharpCore
{
    class Plane : IShape
    {
        IVector<double> Point;
        IVector<double> Normal;
        Material Material;
        Box box;

        Plane() { }

        Plane(IVector<double> point, IVector<double> normal, Material mat)
        {
            Point = point;
            Normal = normal;
            Material = mat;
            box = new Box(new IVector<double>(new double[] { -Util.INF, -Util.INF, -Util.INF, 0 }), new IVector<double>(new double[] { Util.INF, Util.INF, Util.INF, 0 }));
        }

        internal static Plane NewPlane(IVector<double> point, IVector<double> normal, Material material)
        {
            return new Plane(point, normal.Normalize(), material);
        }

        void IShape.Compile() { }

        Box IShape.BoundingBox()
        {
            return new Box(new IVector<double>(new double[] { -Util.INF, -Util.INF, -Util.INF, 0 } ), new IVector<double>(new double[] { Util.INF, Util.INF, Util.INF, 0 }));
        }

        Hit IShape.Intersect(Ray ray)
        {
            double d = Normal.Dot(ray.Direction);
            
            if (Math.Abs(d) < Util.EPS)
            {
                return Hit.NoHit;
            }
            
            IVector<double> a = Point.Sub(ray.Origin);
            double t = a.Dot(Normal) / d;
            
            if (t < Util.EPS)
            {
                return Hit.NoHit;
            }
            
            return new Hit(this, t, null);
        }

        IVector<double> IShape.UV(IVector<double> a)
        {
            return new IVector<double>();
        }

        public Material MaterialAt(IVector<double> v)
        {
            return Material;
        }
        IVector<double> IShape.NormalAt(IVector<double> a)
        {
            return Normal;
        }
    }
}
