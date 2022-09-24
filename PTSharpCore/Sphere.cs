using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Sphere : IShape
    {   
        internal Vector Center;
        internal double Radius;
        internal Material Material;
        internal Box Box;

        Sphere(Vector center_, double radius_, Material material_, Box box_)
        {
            Center = center_;
            Radius = radius_;
            Material = material_;
            Box = box_;
        }
        
        internal static Sphere NewSphere(Vector center, double radius, Material material) 
        {
            var min = new Vector(center.x - radius, center.y - radius, center.z - radius);
            var max = new Vector(center.x + radius, center.y + radius, center.z + radius);
            var box = new Box(min, max);
            return new Sphere(center, radius, material, box);
        }

        Box IShape.BoundingBox()
        {
            return Box;
        }

        Hit IShape.Intersect(Ray r) {
            Vector to = r.Origin.Sub(Center);
            double b = to.Dot(r.Direction);
            double c = to.Dot(to) - Radius * Radius;
            double d = b * b - c;
            if (d > 0)
            {
                d = Math.Sqrt(d);
                double t1 = -b - d;
                if (t1 > Util.EPS)
                {
                    return new Hit(this, t1, null);
                }
                double t2 = -b + d;
                if (t2 > Util.EPS)
                {
                    return new Hit(this, t2, null);
                }
            }
            return Hit.NoHit;
        }

        Vector IShape.UVector(Vector p) {
            p = p.Sub(Center);
            var u = Math.Atan2(p.y, p.x);
            var v = Math.Atan2(p.y, new Vector(p.x, 0, p.y).Length());
            u = 1 - (u + Math.PI) / (2 * Math.PI);
            v = (v + Math.PI / 2) / Math.PI;
            return new Vector(u, v, 0);            
        }
       
        void IShape.Compile() { }
        
        Material IShape.MaterialAt(Vector v)
        {
            return Material;
        }

        Vector IShape.NormalAt(Vector p)
        {
            return p.Sub(Center).Normalize();
        }
    }
}
