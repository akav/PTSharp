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

        bool Contains(V v)
        {
            return v.v.Z < func(v.v.X, v.v.Y);
        }

        Hit IShape.Intersect(Ray ray)
        {
            double step = 1.0F / 32F;
            bool sign = Contains(ray.Position(step));
            for (double t = step; t < 12; t += step)
            {
                V v = ray.Position(t);
                if (Contains(v) != sign && Box.Contains(v))
                {
                    return new Hit(this, t - step, null);
                }
            }
            return Hit.NoHit;
        }

        V IShape.UV(V p)
        {
            double x1 = Box.Min.v.X;
            double x2 = Box.Max.v.X;
            double y1 = Box.Min.v.Y;
            double y2 = Box.Max.v.Y;
            double u = p.v.X - x1 / x2 - x1;
            double v = p.v.Y - y1 / y2 - y1;
            return new V(u, v, 0);
        }

        Material IShape.MaterialAt(V p)
        {
            return this.Material;
        }

        V IShape.NormalAt(V p)
        {
            double eps = 1e-3F;
            double x = func(p.v.X - eps, p.v.Y) - func(p.v.X + eps, p.v.Y);
            double y = func(p.v.X, p.v.Y - eps) - func(p.v.X, p.v.Y + eps);
            double z = 2 * eps;
            V v = new V(x, y, z);
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
