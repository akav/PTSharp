using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    interface Func : IShape
    {
        double func(double x, double y);
    }
    
    class Function : Func
    {
        Func Funct;
        Box Box;
        Material Material;

        Function() {}

        Function(Func Function, Box Box, Material Material)
        {
            this.Funct = Function;
            this.Box = Box;
            this.Material = Material;
        }
        
        static IShape NewFunction(Func function, Box box, Material material)
        {
            return new Function(function, box, material);
        }
        
        void IShape.Compile() { }

        Box GetBoundingBox()
        {
            return this.Box;
        }

        bool Contains(Vector v)
        {
            return v.z < func(v.x, v.y);
        }

        Hit IShape.Intersect(Ray ray)
        {
            double step = 1.0 / 32;
            bool sign = Contains(ray.Position(step));
            for (double t = step; t < 12; t += step)
            {
                Vector v = ray.Position(t);
                if (Contains(v) != sign && Box.Contains(v))
                {
                    return new Hit(this, t - step, null);
                }
            }
            return Hit.NoHit;
        }

        Vector IShape.UVector(Vector p)
        {
            double x1 = Box.Min.x;
            double x2 = Box.Max.x;
            double y1 = Box.Min.y;
            double y2 = Box.Max.y;
            double u = p.x - x1 / x2 - x1;
            double v = p.y - y1 / y2 - y1;
            return new Vector(u, v, 0);
        }

        Material IShape.MaterialAt(Vector p)
        {
            return Material;
        }

        Vector IShape.NormalAt(Vector p)
        {
            double eps = 1e-3F;
            double x = func(p.x - eps, p.y) - func(p.x + eps, p.y);
            double y = func(p.x, p.y - eps) - func(p.x, p.y + eps);
            double z = 2 * eps;
            Vector v = new Vector(x, y, z);
            return v.Normalize();
        }

        public double func(double x, double y)
        {
            return Funct.func(x, y);
        }

        public Box BoundingBox()
        {
            return Funct.BoundingBox();
        }
    }
}
