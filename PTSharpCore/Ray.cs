using System;
using System.Threading;

namespace PTSharpCore
{
    public class Ray
    {
        internal V Origin, Direction;
        internal bool reflect;

        internal Ray(V Origin_, V Direction_)
        {
            Origin = Origin_;
            Direction = Direction_;
        }

        internal V Position(float t) => Origin.Add(Direction.MulScalar(t));

        internal Ray Reflect(Ray i) => new Ray(Origin, Direction.Reflect(i.Direction));

        public Ray Refract(Ray i, float n1, float n2) => new Ray(Origin, Direction.Refract(i.Direction, n1, n2));

        public float Reflectance(Ray i, float n1, float n2) => Direction.Reflectance(i.Direction, n1, n2);

        public Ray WeightedBounce(float u, float v)
        {
            var radius = MathF.Sqrt(u);
            var theta = 2 * MathF.PI * v;
            var s = Direction.Cross(V.RandomUnitVector()).Normalize();
            var t = Direction.Cross(s);
            var d = new V();
            d = d.Add(s.MulScalar(radius * MathF.Cos(theta)));
            d = d.Add(t.MulScalar(radius * MathF.Sin(theta)));
            d = d.Add(Direction.MulScalar(MathF.Sqrt(1 - u)));
            return new Ray(Origin, d);
        }

        public Ray ConeBounce(float theta, float u, float v)
        {
            return new Ray(Origin, Util.Cone(Direction, theta, u, v));
        }

        public (Ray, bool, float) Bounce(HitInfo info, float u, float v, BounceType bounceType)
        {
            var n = info.Ray;
            var material = info.material;
            
            var n1 = 1.0F;
            var n2 = material.Index;
            
            if (info.inside)
            {
                (n1, n2) = (n2, n1);
            }

            float p = material.Reflectivity >= 0 ? material.Reflectivity : n.Reflectance(this, n1, n2);

            switch (bounceType)
            {
                case BounceType.BounceTypeAny:
                    reflect = Random.Shared.NextSingle() < p;
                    break;
                case BounceType.BounceTypeDiffuse:
                    reflect = false;
                    break;
                case BounceType.BounceTypeSpecular:
                    reflect = true;
                    break;
            }

            if (reflect)
            {
                var reflected = n.Reflect(this);
                return (reflected.ConeBounce(material.Gloss, u, v), true, p);
            }
            else if (material.Transparent)
            {
                var refracted = n.Refract(this, n1, n2);
                refracted.Origin = refracted.Origin.Add(refracted.Direction.MulScalar(1e-4F));
                return (refracted.ConeBounce(material.Gloss, u, v), true, 1 - p);
            }
            else
            {
                return (n.WeightedBounce(u, v), false, 1 - p);
            }
        }
    }
}
