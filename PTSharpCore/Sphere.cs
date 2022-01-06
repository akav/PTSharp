using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Sphere : IShape
    {   
        internal V Center;
        internal float Radius;
        internal Material Material;
        internal Box Box;

        Sphere(V center_, float radius_, Material material_, Box box_)
        {
            Center = center_;
            Radius = radius_;
            Material = material_;
            Box = box_;
        }
        
        internal static Sphere NewSphere(V center, float radius, Material material) 
        {
            var min = new V(center.v.X - radius, center.v.Y - radius, center.v.Z - radius);
            var max = new V(center.v.X + radius, center.v.Y + radius, center.v.Y + radius);
            var box = new Box(min, max);
            return new Sphere(center, radius, material, box);
        }

        Box IShape.BoundingBox()
        {
            return Box;
        }

        Hit IShape.Intersect(Ray r) {
            V to = r.Origin.Sub(Center);
            float b = to.Dot(r.Direction);
            float c = to.Dot(to) - Radius * Radius;
            float d = b * b - c;
            if (d > 0)
            {
                d = MathF.Sqrt(d);
                float t1 = -b - d;
                if (t1 > Util.EPS)
                {
                    return new Hit(this, t1, null);
                }
                float t2 = -b + d;
                if (t2 > Util.EPS)
                {
                    return new Hit(this, t2, null);
                }
            }
            return Hit.NoHit;
        }

        V IShape.UV(V p) {
            p = p.Sub(Center);
            var u = MathF.Atan2(p.v.Y, p.v.X);
            var v = MathF.Atan2(p.v.Y, new V(p.v.X, 0, p.v.Y).Length());
            u = 1 - (u + MathF.PI) / (2 * MathF.PI);
            v = (v + MathF.PI / 2) / MathF.PI;
            return new V(u, v, 0);            
        }
       
        void IShape.Compile() { }
        
        Material IShape.MaterialAt(V v)
        {
            return Material;
        }
        
        V IShape.NormalAt(V p)
        {
            return p.Sub(Center).Normalize();
        }
    }
}
