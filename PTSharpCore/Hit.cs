using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    class Hit
    {
        static double INF = 1e9F;
        internal IShape Shape;
        public double T;
        public HitInfo HitInfo;

        internal Hit(IShape shape, double t, HitInfo hinfo)
        {
            Shape = shape;
            T = t;
            HitInfo = hinfo;
        }

        public bool Ok() => T < INF;

        public static Hit NoHit = new Hit(null, INF, null);

        public HitInfo Info(Ray r)
        {
            if (HitInfo != null)
                return HitInfo;
            
            var shape = Shape;
            var position = r.Position(T);
            var normal = shape.NormalAt(position);
            var material = Material.MaterialAt(shape, position);
            var inside = false;
            if (normal.Dot(r.Direction) > 0)
            {
                normal = normal.Negate();
                inside = true;

                switch (shape)
                {
                    case Volume _:
                    case SDFShape _:
                    case SphericalHarmonic _:
                        inside = false;
                        break;
                }
            }
            Ray ray = new Ray(position, normal);
            return new HitInfo(shape, position, normal, ray, material, inside);
        }
    }

    public class HitInfo
    {
        private IShape shape;
        private V position;
        private V normal;
        public Ray Ray;
        internal Material material;
        public bool inside;

        internal HitInfo(IShape shape, V position, V normal, Ray r, Material mat, bool inside)
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