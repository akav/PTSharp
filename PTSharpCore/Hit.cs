using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Hit
    {
        static double INF = 1e9F;

        internal IShape Shape;
        public double T; // Distance
        public HitInfo HitInfo;

        internal Hit(IShape shape, double t, HitInfo hinfo)
        {
            Shape = shape;
            T = t;
            HitInfo = hinfo; //?? Info(new Ray(Vector.Zero, Vector.Zero)); // Ensure HitInfo is initialized
        }

        public Hit()
        {
        }

        public bool Ok => T < INF;

        public static Hit NoHit = new Hit(null, INF, null);

        public HitInfo Info(Ray r)
        {
            if (HitInfo is not null)
                return HitInfo;
            
            var shape = Shape;
            var position = r.Position(T);
            var normal = shape.NormalAt(position);
            var material = Material.MaterialAt(shape,position);
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
        public IShape Shape;
        public Vector Position;
        public Vector Normal;
        public Ray Ray;
        public Material material;
        public bool Inside;

        internal HitInfo(IShape shape, Vector position, Vector normal, Ray r, Material mat, bool inside)
        {
            this.Shape = shape;
            this.Position = position;
            this.Normal = normal;
            this.Ray = r;
            this.material = mat;
            this.Inside = inside;
        }
    }
}