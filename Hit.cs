using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharp
{
    class Hit
    {
        static double INF = 1e9;
        internal IShape Shape;
        public double T;
        public HitInfo HitInfo;

        public static Hit NoHit = new Hit(null, INF, null);

        internal Hit(IShape shape, double t, HitInfo hinfo)
        {
            Shape = shape;
            T = t;
            HitInfo = hinfo;
        }

        public Boolean Ok() => T < INF;

        public HitInfo Info(Ray r)
        {
            if (HitInfo != null)
            {
                return HitInfo;
            }
            IShape shape = Shape;
            Vector position = r.Position(T);
            Vector normal = this.Shape.NormalAt(position);
            Material material = Material.MaterialAt(this.Shape, position);
            Boolean inside = false;
            if (normal.Dot(r.Direction) > 0)
            {
                normal = normal.Negate();
                inside = true;
                if(shape is Volume)
                {
                    inside = false;
                }
            }
            Ray ray = new Ray(position, normal);
            return new HitInfo(Shape, position, normal, ray, material, inside);
        }
    }

    public class HitInfo
    {
        private IShape shape;
        private Vector position;
        private Vector normal;
        public Ray Ray;
        internal Material material;
        public Boolean inside;

        internal HitInfo(IShape shape, Vector position, Vector normal, Ray r, Material mat, Boolean inside)
        {
            this.shape = shape;
            this.position = position;
            this.normal = normal;
            this.Ray = r;
            this.material = mat;
            this.inside = inside;
        }
    }
}
