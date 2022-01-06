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

        bool Contains(IVector<double> v)
        {
            return v.dv[2] < func(v.dv[0], v.dv[1]);
        }

        Hit IShape.Intersect(Ray ray)
        {
            double step = 1.0 / 32;
            bool sign = Contains(ray.Position(step));
            for (double t = step; t < 12; t += step)
            {
                IVector<double> v = ray.Position(t);
                if (Contains(v) != sign && Box.Contains(v))
                {
                    return new Hit(this, t - step, null);
                }
            }
            return Hit.NoHit;
        }

        IVector<double> IShape.UV(IVector<double> p)
        {
            double x1 = Box.Min.dv[0];
            double x2 = Box.Max.dv[0];
            double y1 = Box.Min.dv[1];
            double y2 = Box.Max.dv[1];
            double u = p.dv[0] - x1 / x2 - x1;
            double v = p.dv[1] - y1 / y2 - y1;
            return new IVector<double>(new double[] { u, v, 0, 0 });
        }

        Material IShape.MaterialAt(IVector<double> p)
        {
            return this.Material;
        }

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            double eps = 1e-3;
            double x = func(p.dv[0] - eps, p.dv[1]) - func(p.dv[0] + eps, p.dv[1]);
            double y = func(p.dv[0], p.dv[1] - eps) - func(p.dv[0], p.dv[1] + eps);
            double z = 2 * eps;
            IVector<double> v = new IVector<double>(new double[] { x, y, z, 0 });
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
