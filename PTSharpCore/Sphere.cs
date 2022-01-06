using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Sphere : IShape
    {   
        internal IVector<double> Center;
        internal double Radius;
        internal Material Material;
        internal Box Box;

        Sphere(IVector<double> center_, double radius_, Material material_, Box box_)
        {
            Center = center_;
            Radius = radius_;
            Material = material_;
            Box = box_;
        }
        
        internal static Sphere NewSphere(IVector<double> center, double radius, Material material) 
        {
            var min = new IVector<double>(new double[] { center.dv[0] - radius, center.dv[1] - radius, center.dv[2] - radius, 0 });
            var max = new IVector<double>(new double[] { center.dv[0] + radius, center.dv[1] + radius, center.dv[2] + radius, 0 });
            var box = new Box(min, max);
            return new Sphere(center, radius, material, box);
        }

        Box IShape.BoundingBox()
        {
            return Box;
        }

        Hit IShape.Intersect(Ray r) {
            IVector<double> to = r.Origin.Sub(Center);
            var b = to.Dot(r.Direction);
            var c = to.Dot(to) - Radius * Radius;
            var d = b * b - c;
            if (d > 0)
            {
                d = Math.Sqrt(d);
                var t1 = -b - d;
                if (t1 > Util.EPS)
                {
                    return new Hit(this, t1, null);
                }
                var t2 = -b + d;
                if (t2 > Util.EPS)
                {
                    return new Hit(this, t2, null);
                }
            }
            return Hit.NoHit;
        }

        IVector<double> IShape.UV(IVector<double> p) {
            p = p.Sub(Center);
            var u = Math.Atan2(p.dv[2], p.dv[0]);
            var v = Math.Atan2(p.dv[1], new IVector<double>(new double[] { p.dv[0], 0, p.dv[2], 0 }).Length());
            u = 1 - (u + Math.PI) / (2 * Math.PI);
            v = (v + Math.PI / 2) / Math.PI;
            return new IVector<double>(new double[] { u, v, 0, 0 });            
        }
       
        void IShape.Compile() { }
        
        Material IShape.MaterialAt(IVector<double> v)
        {
            return Material;
        }
        
        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            return p.Sub(Center).Normalize();
        }
    }
}
